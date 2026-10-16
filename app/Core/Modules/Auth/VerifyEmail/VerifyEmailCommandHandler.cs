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

namespace Modules.Auth.VerifyEmail;

public sealed class VerifyEmailCommandHandler(
    IRepository<User> userRepository,
    IUnitOfWork unitOfWork,
    ISecretHashService secretHashService,
    ILogger<VerifyEmailCommandHandler> logger) : IRequestHandler<VerifyEmailCommand, VerifyEmailResponseDto>
{
    private readonly IRepository<User> _userRepository = userRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ISecretHashService _secretHashService = secretHashService;
    private readonly ILogger<VerifyEmailCommandHandler> _logger = logger;

    public async Task<VerifyEmailResponseDto> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var emailHash = _secretHashService.ComputeSha256(email);

        var user = await _userRepository.FirstOrDefaultAsync(new UserByEmailSpec(email), cancellationToken);
        if (user is null || !user.IsActive)
        {
            _logger.LogInformation("Email verification failed: account unavailable. EmailHash: {EmailHash}", emailHash);
            throw new ResourceNotFoundException("Account was not found.");
        }

        if (user.EmailConfirmed)
        {
            return new VerifyEmailResponseDto
            {
                Message = "Email is already verified.",
                Verified = true,
                NextAction = "login"
            };
        }

        if (string.IsNullOrWhiteSpace(user.EmailVerificationCode)
            || user.EmailVerificationCodeExpiryTime is null
            || user.EmailVerificationCodeExpiryTime <= DateTimeOffset.UtcNow)
        {
            _logger.LogInformation("Email verification failed: code missing or expired. UserId: {UserId}", user.Id);
            throw InvalidOtpCodeException();
        }

        var incomingCodeHash = _secretHashService.ComputeSha256(request.OtpCode.Trim());
        if (!string.Equals(user.EmailVerificationCode, incomingCodeHash, StringComparison.Ordinal))
        {
            _logger.LogInformation("Email verification failed: code mismatch. UserId: {UserId}", user.Id);
            throw InvalidOtpCodeException();
        }

        user.EmailConfirmed = true;
        user.EmailVerificationCode = null;
        user.EmailVerificationCodeExpiryTime = null;
        user.LastModifiedBy = user.Id.ToString();
        user.LastModifiedOn = DateTimeOffset.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Email verification succeeded. UserId: {UserId}, EmailHash: {EmailHash}", user.Id, emailHash);

        return new VerifyEmailResponseDto
        {
            Message = "Email verified successfully.",
            Verified = true,
            NextAction = "login"
        };
    }

    private static ValidationException InvalidOtpCodeException()
        => new([new ValidationFailure(nameof(VerifyEmailCommand.OtpCode), "Verification code is invalid or expired.")]);
}
