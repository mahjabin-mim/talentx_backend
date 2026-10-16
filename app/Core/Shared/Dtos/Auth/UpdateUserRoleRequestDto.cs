namespace Shared.Dtos.Auth;

public sealed class UpdateUserRoleRequestDto
{
    public string UserRole { get; init; } = string.Empty;
    public string? Fullname { get; init; }
    public string? CompanyName { get; init; }
}
