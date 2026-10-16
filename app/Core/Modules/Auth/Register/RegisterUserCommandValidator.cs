using FluentValidation;

namespace Modules.Auth.Register;

public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Fullname)
            .NotEmpty()
            .MaximumLength(120);

        RuleFor(x => x.CompanyName)
            .NotEmpty()
            .MaximumLength(160);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(254);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(128)
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\w\s]).{8,128}$")
            .WithMessage("Password must contain uppercase, lowercase, number, and special character.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .MaximumLength(20);
    }
}
