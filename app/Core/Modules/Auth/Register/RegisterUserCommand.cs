using MediatR;
using Shared.Dtos.Auth;

namespace Modules.Auth.Register;

public sealed record RegisterUserCommand(
    string Email,
    string Password,
    string Fullname,
    string CompanyName,
    string PhoneNumber) : IRequest<RegisterResponseDto>;
