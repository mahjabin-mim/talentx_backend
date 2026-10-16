using FluentValidation;

namespace Modules.Jobs.GetEmployerDashboard;

public sealed class GetEmployerDashboardQueryValidator : AbstractValidator<GetEmployerDashboardQuery>
{
    public GetEmployerDashboardQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty);
    }
}
