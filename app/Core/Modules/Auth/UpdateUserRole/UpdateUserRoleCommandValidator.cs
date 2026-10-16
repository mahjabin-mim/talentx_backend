using Domain.Enums;
using FluentValidation;

namespace Modules.Auth.UpdateUserRole;

public sealed class UpdateUserRoleCommandValidator : AbstractValidator<UpdateUserRoleCommand>
{
    public UpdateUserRoleCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty);

        RuleFor(x => x.UserRole)
            .NotEmpty()
            .MaximumLength(20)
            .Must(BeSupportedRole)
            .WithMessage("Unsupported role value.");

        RuleFor(x => x.Fullname)
            .MaximumLength(120)
            .When(x => !string.IsNullOrWhiteSpace(x.Fullname));

        RuleFor(x => x.CompanyName)
            .MaximumLength(160)
            .When(x => !string.IsNullOrWhiteSpace(x.CompanyName));
    }

    private static bool BeSupportedRole(string role)
        => Enum.TryParse<UserRole>(role, true, out _);
}
