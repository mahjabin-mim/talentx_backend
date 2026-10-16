using MediatR;
using Shared.Dtos.TalentDashboard;

namespace Modules.TalentDashboard.GetApplications;

public sealed record GetTalentDashboardApplicationsQuery(
    Guid UserId,
    int ApplicationsPage,
    int InvitationsPage,
    int PageSize)
    : IRequest<GetTalentDashboardApplicationsResponseDto>;
