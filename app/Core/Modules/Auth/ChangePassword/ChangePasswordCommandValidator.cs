using FluentValidation;

namespace Modules.Auth.ChangePassword;

public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId is required.");

        RuleFor(x => x.CurrentPassword)
            .NotEmpty()
            .WithMessage("Current password is required.");

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithMessage("New password is required.")
            .MinimumLength(9)
            .WithMessage("Password length must be greater than 8.")
            .Matches("[a-z]")
            .WithMessage("Password must contain at least 1 lowercase letter.")
            .Matches("[A-Z]")
            .WithMessage("Password must contain at least 1 uppercase letter.")
            .Matches("[0-9]")
            .WithMessage("Password must contain at least 1 number.")
            .Matches("[^A-Za-z0-9]")
            .WithMessage("Password must contain at least 1 special character.");

        RuleFor(x => x)
            .Must(x => x.CurrentPassword != x.NewPassword)
            .WithMessage("Current password and new password cannot be the same.");
    }
}
