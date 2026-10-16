using MediatR;
using Shared.Dtos.Auth;

namespace Modules.Auth.Login;

public sealed record LoginUserCommand(
    string Email,
    string Password) : IRequest<AuthResponseDto>;
