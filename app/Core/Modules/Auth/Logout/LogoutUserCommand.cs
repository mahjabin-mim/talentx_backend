using MediatR;

namespace Modules.Auth.Logout;

public sealed record LogoutUserCommand(Guid UserId) : IRequest;
