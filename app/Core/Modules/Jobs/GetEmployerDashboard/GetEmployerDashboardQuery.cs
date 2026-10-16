using MediatR;
using Shared.Dtos.Jobs;

namespace Modules.Jobs.GetEmployerDashboard;

public sealed record GetEmployerDashboardQuery(
    Guid UserId,
    string? SearchTerm = null,
    string? StatusFilter = null,
    int Page = 1,
    int PageSize = 10) : IRequest<GetEmployerDashboardResponseDto>;