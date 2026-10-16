using FluentValidation;

namespace Modules.Jobs.DeleteJob;

public sealed class DeleteJobCommandValidator : AbstractValidator<DeleteJobCommand>
{
    public DeleteJobCommandValidator()
    {
        RuleFor(x => x.UserId).NotEqual(Guid.Empty);
        RuleFor(x => x.JobId).NotEqual(Guid.Empty);
    }
}
