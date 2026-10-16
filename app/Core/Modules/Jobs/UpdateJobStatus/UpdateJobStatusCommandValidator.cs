using FluentValidation;

namespace Modules.Jobs.UpdateJobStatus;

public sealed class UpdateJobStatusCommandValidator : AbstractValidator<UpdateJobStatusCommand>
{
    public UpdateJobStatusCommandValidator()
    {
        RuleFor(x => x.UserId).NotEqual(Guid.Empty);
        RuleFor(x => x.JobId).NotEqual(Guid.Empty);
        RuleFor(x => x.Action)
            .NotEmpty()
            .Must(x => x.Equals("Activate", StringComparison.OrdinalIgnoreCase)
                || x.Equals("Deactivate", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Action must be Activate or Deactivate.");
    }
}
