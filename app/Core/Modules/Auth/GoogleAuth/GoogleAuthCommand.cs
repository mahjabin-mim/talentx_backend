using MediatR;
using Shared.Dtos.Auth;

namespace Modules.Auth.GoogleAuth;

public sealed record GoogleAuthCommand(
    string Email,
    string? Fullname,
    string? ProfileImageURL) : IRequest<AuthResponseDto>;
