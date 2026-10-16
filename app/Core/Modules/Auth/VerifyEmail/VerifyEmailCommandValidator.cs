using FluentValidation;

namespace Modules.Auth.VerifyEmail;

public sealed class VerifyEmailCommandValidator : AbstractValidator<VerifyEmailCommand>
{
    public VerifyEmailCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(254);

        RuleFor(x => x.OtpCode)
            .NotEmpty()
            .Length(6)
            .Matches(@"^\d{6}$")
            .WithMessage("'Otp Code' must be a 6 digit code.");
    }
}
