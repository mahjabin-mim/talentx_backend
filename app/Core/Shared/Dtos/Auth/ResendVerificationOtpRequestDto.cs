namespace Shared.Dtos.Auth;

public sealed class ResendVerificationOtpRequestDto
{
    public string Email { get; init; } = string.Empty;
}
