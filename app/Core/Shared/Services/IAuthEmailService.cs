namespace Shared.Services;

public interface IAuthEmailService
{
    Task SendEmailVerificationOtpAsync(
        string email,
        string fullName,
        string otpCode,
        DateTimeOffset expiresAtUtc,
        CancellationToken cancellationToken = default);
}
