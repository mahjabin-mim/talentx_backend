using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Domain.Specifications;
using Infrastructure.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Dtos.Auth;
using Shared.Exceptions;

namespace Modules.Auth.Login;

public sealed class LoginUserCommandHandler(
    IRepository<User> userRepository,
    IRepository<Application> applicationRepository,
    IUnitOfWork unitOfWork,
    IPasswordService passwordService,
    ISecretHashService secretHashService,
    ITokenService tokenService,
    ILogger<LoginUserCommandHandler> logger) : IRequestHandler<LoginUserCommand, AuthResponseDto>
{
    private readonly IRepository<User> _userRepository = userRepository;
    private readonly IRepository<Application> _applicationRepository = applicationRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IPasswordService _passwordService = passwordService;
    private readonly ISecretHashService _secretHashService = secretHashService;
    private readonly ITokenService _tokenService = tokenService;
    private readonly ILogger<LoginUserCommandHandler> _logger = logger;

    public async Task<AuthResponseDto> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var emailHash = _secretHashService.ComputeSha256(email);

        _logger.LogInformation(
            "Login request received. EmailHash: {EmailHash}",
            emailHash);

        var user = await _userRepository.FirstOrDefaultAsync(new UserByEmailSpec(email), cancellationToken);

        if (user is null || !_passwordService.VerifyPassword(request.Password, user.Password))
        {
            _logger.LogInformation(
                "Login failed due to invalid credentials. EmailHash: {EmailHash}",
                emailHash);
            throw new UnauthorizedException("Invalid email or password.");
        }

        if (!user.IsActive)
        {
            _logger.LogInformation(
                "Login blocked: user account is inactive. UserId: {UserId}, EmailHash: {EmailHash}",
                user.Id,
                emailHash);
            throw new UnauthorizedException("This account is inactive.");
        }

        if (!user.EmailConfirmed)
        {
            _logger.LogInformation(
                "Login blocked: email is not verified. UserId: {UserId}, EmailHash: {EmailHash}",
                user.Id,
                emailHash);
            throw new EmailVerificationRequiredException("Email verification is required before sign in.");
        }

        if (user.UserRole is null)
        {
            var hasApplicationHistory = await _applicationRepository.AnyAsync(new ApplicationByEmailSpec(email), cancellationToken);

            if (hasApplicationHistory)
            {
                user.UserRole = UserRole.Talent;
                user.LastModifiedOn = DateTimeOffset.UtcNow;
                user.LastModifiedBy = user.Id.ToString();

                _logger.LogInformation(
                    "Recovered missing talent role from application history. UserId: {UserId}, EmailHash: {EmailHash}",
                    user.Id,
                    emailHash);
            }
        }

        var refreshToken = _tokenService.CreateRefreshToken();
        user.RefreshToken = _secretHashService.ComputeSha256(refreshToken);
        user.RefreshTokenExpiryTime = _tokenService.GetRefreshTokenExpiry();
        user.LastModifiedOn = DateTimeOffset.UtcNow;
        user.LastModifiedBy = user.Id.ToString();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var accessToken = _tokenService.CreateAccessToken(user);

        _logger.LogInformation(
            "Login succeeded. UserId: {UserId}, EmailHash: {EmailHash}, Role: {Role}",
            user.Id,
            emailHash,
            user.UserRole);

        return new AuthResponseDto
        {
            Id = user.Id,
            Fullname = user.Fullname,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            UserRole = user.UserRole?.ToString(),
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            RefreshTokenExpiryTime = user.RefreshTokenExpiryTime ?? DateTimeOffset.UtcNow,
            Message = "Sign in successful."
        };
    }
}
