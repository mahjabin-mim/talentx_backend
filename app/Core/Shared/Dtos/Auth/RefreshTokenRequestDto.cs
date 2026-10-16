namespace Shared.Dtos.Auth;

public sealed class RefreshTokenRequestDto
{
    public Guid UserId { get; init; }

    public string RefreshToken { get; init; } = string.Empty;
}
