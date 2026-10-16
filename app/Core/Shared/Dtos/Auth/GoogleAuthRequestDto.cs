namespace Shared.Dtos.Auth;

public sealed class GoogleAuthRequestDto
{
    public string Email { get; init; } = string.Empty;

    public string? Fullname { get; init; }

    public string? ProfileImageURL { get; init; }
}
