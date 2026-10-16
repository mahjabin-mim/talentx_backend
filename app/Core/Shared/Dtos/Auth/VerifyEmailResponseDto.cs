namespace Shared.Dtos.Auth;

public sealed class VerifyEmailResponseDto
{
    public string Message { get; init; } = string.Empty;

    public bool Verified { get; init; }

    public string NextAction { get; init; } = string.Empty;
}
