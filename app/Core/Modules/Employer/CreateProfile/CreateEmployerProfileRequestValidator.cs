using FluentValidation;

namespace Modules.Employer.CreateProfile;

public sealed class CreateEmployerProfileRequestValidator : AbstractValidator<CreateEmployerProfileRequestDto>
{
    public CreateEmployerProfileRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(100);
        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("Company name is required.")
            .MaximumLength(160);
    }
}
