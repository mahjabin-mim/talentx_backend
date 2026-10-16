using MediatR;
using Shared.Dtos.Auth;

namespace Modules.Auth.RefreshToken;

public sealed record RefreshTokenCommand(
    Guid UserId,
    string RefreshToken) : IRequest<AuthResponseDto>;
