using FluentValidation;

namespace Modules.Auth.Logout;

public sealed class LogoutUserCommandValidator : AbstractValidator<LogoutUserCommand>
{
    public LogoutUserCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty);
    }
}
