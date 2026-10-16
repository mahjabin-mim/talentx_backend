using FluentValidation;

namespace Modules.Jobs.GetJobById;

public sealed class GetJobByIdQueryValidator : AbstractValidator<GetJobByIdQuery>
{
    public GetJobByIdQueryValidator()
    {
        RuleFor(x => x.JobId).NotEqual(Guid.Empty);
    }
}
