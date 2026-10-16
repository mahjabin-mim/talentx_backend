using FluentValidation;

namespace Modules.Auth.ResendVerificationOtp;

public sealed class ResendVerificationOtpCommandValidator : AbstractValidator<ResendVerificationOtpCommand>
{
    public ResendVerificationOtpCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(254);
    }
}
