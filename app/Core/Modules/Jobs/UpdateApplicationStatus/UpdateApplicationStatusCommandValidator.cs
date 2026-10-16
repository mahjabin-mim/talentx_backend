using Domain.Enums;
using FluentValidation;

namespace Modules.Jobs.UpdateApplicationStatus;

public sealed class UpdateApplicationStatusCommandValidator : AbstractValidator<UpdateApplicationStatusCommand>
{
    public UpdateApplicationStatusCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(status => Enum.TryParse<ApplicationStatus>(status, true, out _))
            .WithMessage("Invalid application status.");
    }
}
