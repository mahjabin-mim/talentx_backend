using MediatR;
using Shared.Dtos.Auth;

namespace Modules.Auth.ResendVerificationOtp;

public sealed record ResendVerificationOtpCommand(
    string Email) : IRequest<ResendVerificationOtpResponseDto>;
