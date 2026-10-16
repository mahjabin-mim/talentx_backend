using FluentValidation;

namespace Modules.Auth.GoogleAuth;

public sealed class GoogleAuthCommandValidator : AbstractValidator<GoogleAuthCommand>
{
    public GoogleAuthCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(254);

        RuleFor(x => x.Fullname)
            .MaximumLength(120)
            .When(x => !string.IsNullOrWhiteSpace(x.Fullname));

        RuleFor(x => x.ProfileImageURL)
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("'Profile Image URL' is not a valid URL.")
            .When(x => !string.IsNullOrWhiteSpace(x.ProfileImageURL));

        RuleFor(x => x.ProfileImageURL)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.ProfileImageURL));
    }
}
