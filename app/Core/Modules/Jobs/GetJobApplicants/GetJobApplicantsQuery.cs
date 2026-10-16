using MediatR;
using Shared.Dtos.Jobs;

namespace Modules.Jobs.GetJobApplicants;

public sealed record GetJobApplicantsQuery(
    Guid JobId,
    Guid UserId,
    int Page = 1,
    int PageSize = 10,
    string? Status = null,
    string? SearchTerm = null
) : IRequest<GetJobApplicantsResponseDto>;
