using System.Security.Cryptography;
using Domain.Entities;
using Domain.Repositories;
using Domain.Specifications;
using Infrastructure.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Dtos.Auth;
using Shared.Exceptions;

namespace Modules.Auth.GoogleAuth;

public sealed class GoogleAuthCommandHandler(
    IRepository<User> userRepository,
    IUnitOfWork unitOfWork,
    ISecretHashService secretHashService,
    ITokenService tokenService,
    ILogger<GoogleAuthCommandHandler> logger) : IRequestHandler<GoogleAuthCommand, AuthResponseDto>
{
    private readonly IRepository<User> _userRepository = userRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ISecretHashService _secretHashService = secretHashService;
    private readonly ITokenService _tokenService = tokenService;
    private readonly ILogger<GoogleAuthCommandHandler> _logger = logger;

    public async Task<AuthResponseDto> Handle(GoogleAuthCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var emailHash = _secretHashService.ComputeSha256(email);

        var user = await _userRepository.FirstOrDefaultAsync(new UserByEmailSpec(email), cancellationToken);
        var created = false;

        if (user is null)
        {
            created = true;
            var dummyPasswordBytes = RandomNumberGenerator.GetBytes(32);
            var dummyPassword = $"OAUTH_ONLY_{Convert.ToBase64String(dummyPasswordBytes)}";

            user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                Fullname = string.IsNullOrWhiteSpace(request.Fullname) ? null : request.Fullname.Trim(),
                ProfileImageURL = string.IsNullOrWhiteSpace(request.ProfileImageURL) ? null : request.ProfileImageURL.Trim(),
                Password = dummyPassword,
                IsActive = true,
                EmailConfirmed = true,
                CreatedBy = "google-oauth",
                CreatedOn = DateTimeOffset.UtcNow,
                Deleted = false,
                TokenVersion = 0
            };
            await _userRepository.AddAsync(user, cancellationToken);
        }
        else
        {
            if (!user.IsActive)
            {
                _logger.LogInformation(
                    "Google auth blocked: inactive account. UserId: {UserId}, EmailHash: {EmailHash}",
                    user.Id,
                    emailHash);
                throw new UnauthorizedException("This account is inactive.");
            }

            if (!string.IsNullOrWhiteSpace(request.Fullname)) user.Fullname = request.Fullname.Trim();
            if (!string.IsNullOrWhiteSpace(request.ProfileImageURL)) user.ProfileImageURL = request.ProfileImageURL.Trim();
        }

        user.EmailConfirmed = true;
        var refreshToken = _tokenService.CreateRefreshToken();
        user.RefreshToken = _secretHashService.ComputeSha256(refreshToken);
        user.RefreshTokenExpiryTime = _tokenService.GetRefreshTokenExpiry();
        user.LastModifiedOn = DateTimeOffset.UtcNow;
        user.LastModifiedBy = user.Id.ToString();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var accessToken = _tokenService.CreateAccessToken(user);

        _logger.LogInformation(
            "Google auth succeeded. UserId: {UserId}, EmailHash: {EmailHash}, Created: {Created}",
            user.Id,
            emailHash,
            created);

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
            Message = created ? "Google sign-up successful." : "Google sign-in successful."
        };
    }
}
