using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Domain.Specifications;
using Infrastructure.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Dtos.Auth;
using Shared.Exceptions;
using Shared.Services;

namespace Modules.Auth.Register;

public sealed class RegisterUserCommandHandler(
    IRepository<User> userRepository,
    IRepository<Domain.Entities.Employer> employerRepository,
    IUnitOfWork unitOfWork,
    IPasswordService passwordService,
    ISecretHashService secretHashService,
    ITokenService tokenService,
    IAuthEmailService authEmailService,
    ILogger<RegisterUserCommandHandler> logger) : IRequestHandler<RegisterUserCommand, RegisterResponseDto>
{
    private readonly IRepository<User> _userRepository = userRepository;
    private readonly IRepository<Domain.Entities.Employer> _employerRepository = employerRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IPasswordService _passwordService = passwordService;
    private readonly ISecretHashService _secretHashService = secretHashService;
    private readonly ITokenService _tokenService = tokenService;
    private readonly IAuthEmailService _authEmailService = authEmailService;
    private readonly ILogger<RegisterUserCommandHandler> _logger = logger;

    public async Task<RegisterResponseDto> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var emailHash = _secretHashService.ComputeSha256(email);

        _logger.LogInformation(
            "Registration request received. EmailHash: {EmailHash}",
            emailHash);

        if (await _userRepository.AnyAsync(new UserExistsByEmailSpec(email), cancellationToken))
        {
            _logger.LogInformation(
                "Registration conflict: email already exists. EmailHash: {EmailHash}",
                emailHash);
            throw new RegisterConflictException("Email is already registered.");
        }

        var emailVerificationCode = _tokenService.CreateEmailVerificationCode();
        var emailVerificationCodeExpiry = _tokenService.GetEmailVerificationCodeExpiry();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Fullname = request.Fullname.Trim(),
            Email = email,
            Password = _passwordService.HashPassword(request.Password),
            PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
            IsActive = true,
            EmailConfirmed = false,
            EmailVerificationCode = _secretHashService.ComputeSha256(emailVerificationCode),
            EmailVerificationCodeExpiryTime = emailVerificationCodeExpiry,
            CreatedBy = "system",
            CreatedOn = DateTimeOffset.UtcNow,
            Deleted = false,
            TokenVersion = 0,
            UserRole = UserRole.Employer
        };

        await _userRepository.AddAsync(user, cancellationToken);

        await _employerRepository.AddAsync(new Domain.Entities.Employer
        {
            UserId = user.Id,
            User = user,
            CompanyName = request.CompanyName.Trim()
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _authEmailService.SendEmailVerificationOtpAsync(
            user.Email,
            user.Fullname,
            emailVerificationCode,
            emailVerificationCodeExpiry,
            cancellationToken);

        _logger.LogInformation(
            "Registration succeeded. UserId: {UserId}, EmailHash: {EmailHash}, Role: {Role}",
            user.Id,
            emailHash,
            user.UserRole);

        return new RegisterResponseDto
        {
            Message = "Registration completed. Please verify your email with the OTP code.",
            RequiresVerification = true,
            NextAction = "verify_email",
            VerificationCodeExpiresAtUtc = emailVerificationCodeExpiry
        };
    }
}
