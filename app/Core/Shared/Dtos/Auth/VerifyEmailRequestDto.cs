namespace Shared.Dtos.Auth;

public sealed class VerifyEmailRequestDto
{
    public string Email { get; init; } = string.Empty;

    public string OtpCode { get; init; } = string.Empty;
}
