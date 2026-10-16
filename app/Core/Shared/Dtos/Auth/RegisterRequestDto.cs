namespace Shared.Dtos.Auth;

public sealed class RegisterRequestDto
{
    public string Fullname { get; init; } = string.Empty;

    public string CompanyName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string PhoneNumber { get; init; } = string.Empty;
}
