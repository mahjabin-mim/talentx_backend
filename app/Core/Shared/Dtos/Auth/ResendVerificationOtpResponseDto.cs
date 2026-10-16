namespace Shared.Dtos.Auth;

public sealed class ResendVerificationOtpResponseDto
{
    public string Message { get; init; } = string.Empty;

    public bool RequiresVerification { get; init; }

    public string NextAction { get; init; } = string.Empty;

    public DateTimeOffset VerificationCodeExpiresAtUtc { get; init; }
}
