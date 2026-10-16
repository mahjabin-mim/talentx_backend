using FluentValidation;

namespace Modules.TalentDashboard.GetApplications;

public sealed class GetTalentDashboardApplicationsQueryValidator
    : AbstractValidator<GetTalentDashboardApplicationsQuery>
{
    public GetTalentDashboardApplicationsQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId is required.");

        RuleFor(x => x.ApplicationsPage)
            .GreaterThan(0)
            .WithMessage("ApplicationsPage must be greater than 0.");

        RuleFor(x => x.InvitationsPage)
            .GreaterThan(0)
            .WithMessage("InvitationsPage must be greater than 0.");

        RuleFor(x => x.PageSize)
            .Equal(5)
            .WithMessage("PageSize must be 5.");
    }
}
