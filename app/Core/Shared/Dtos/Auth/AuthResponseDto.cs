namespace Shared.Dtos.Auth;

public sealed class AuthResponseDto
{
    public Guid Id { get; init; }
    public string? Fullname { get; init; }
    public string Email { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public string? UserRole { get; init; }
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public DateTimeOffset RefreshTokenExpiryTime { get; init; }
    public string Message { get; init; } = string.Empty;
}
