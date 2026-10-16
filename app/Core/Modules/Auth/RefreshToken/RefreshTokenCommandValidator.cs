using FluentValidation;

namespace Modules.Auth.RefreshToken;

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty);

        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .MaximumLength(256);
    }
}
