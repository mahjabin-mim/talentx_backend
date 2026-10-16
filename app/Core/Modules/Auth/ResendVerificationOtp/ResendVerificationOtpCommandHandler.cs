using Domain.Entities;
using Domain.Repositories;
using Domain.Specifications;
using FluentValidation;
using FluentValidation.Results;
using Infrastructure.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Dtos.Auth;
using Shared.Exceptions;
using Shared.Services;

namespace Modules.Auth.ResendVerificationOtp;

public sealed class ResendVerificationOtpCommandHandler(
    IRepository<User> userRepository,
    IUnitOfWork unitOfWork,
    ISecretHashService secretHashService,
    ITokenService tokenService,
    IAuthEmailService authEmailService,
    ILogger<ResendVerificationOtpCommandHandler> logger) : IRequestHandler<ResendVerificationOtpCommand, ResendVerificationOtpResponseDto>
{
    private readonly IRepository<User> _userRepository = userRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ISecretHashService _secretHashService = secretHashService;
    private readonly ITokenService _tokenService = tokenService;
    private readonly IAuthEmailService _authEmailService = authEmailService;
    private readonly ILogger<ResendVerificationOtpCommandHandler> _logger = logger;

    public async Task<ResendVerificationOtpResponseDto> Handle(ResendVerificationOtpCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var emailHash = _secretHashService.ComputeSha256(email);

        var user = await _userRepository.FirstOrDefaultAsync(new UserByEmailSpec(email), cancellationToken);
        if (user is null || !user.IsActive)
        {
            _logger.LogInformation("Resend verification OTP failed: account unavailable. EmailHash: {EmailHash}", emailHash);
            throw new ResourceNotFoundException("Account was not found.");
        }

        if (user.EmailConfirmed)
        {
            throw new ValidationException([
                new ValidationFailure(nameof(ResendVerificationOtpCommand.Email), "Email is already verified.")
            ]);
        }

        var emailVerificationCode = _tokenService.CreateEmailVerificationCode();
        var emailVerificationCodeExpiry = _tokenService.GetEmailVerificationCodeExpiry();

        user.EmailVerificationCode = _secretHashService.ComputeSha256(emailVerificationCode);
        user.EmailVerificationCodeExpiryTime = emailVerificationCodeExpiry;
        user.LastModifiedBy = user.Id.ToString();
        user.LastModifiedOn = DateTimeOffset.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _authEmailService.SendEmailVerificationOtpAsync(
            user.Email,
            user.Fullname ?? user.Email,
            emailVerificationCode,
            emailVerificationCodeExpiry,
            cancellationToken);

        _logger.LogInformation("Verification OTP resent. UserId: {UserId}, EmailHash: {EmailHash}", user.Id, emailHash);

        return new ResendVerificationOtpResponseDto
        {
            Message = "Verification OTP sent. Please verify your email with the OTP code.",
            RequiresVerification = true,
            NextAction = "verify_email",
            VerificationCodeExpiresAtUtc = emailVerificationCodeExpiry
        };
    }
}
