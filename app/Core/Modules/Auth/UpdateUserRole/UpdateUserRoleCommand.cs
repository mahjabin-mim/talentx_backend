using MediatR;
using Shared.Dtos.Auth;

namespace Modules.Auth.UpdateUserRole;

public sealed record UpdateUserRoleCommand(
    Guid UserId,
    string UserRole,
    string? Fullname,
    string? CompanyName) : IRequest<AuthResponseDto>;
