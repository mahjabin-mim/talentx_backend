using MediatR;
using Shared.Dtos.Auth;

namespace Modules.Auth.VerifyEmail;

public sealed record VerifyEmailCommand(
    string Email,
    string OtpCode) : IRequest<VerifyEmailResponseDto>;
