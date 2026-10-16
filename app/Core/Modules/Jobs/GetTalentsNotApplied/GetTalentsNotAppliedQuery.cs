using MediatR;
using Shared.Dtos.Jobs;

namespace Modules.Jobs.GetTalentsNotApplied;

public sealed record GetTalentsNotAppliedQuery(
    Guid JobId,
    Guid UserId,
    int Page = 1,
    int PageSize = 10,
    string? SearchTerm = null,
    int? MinExperience = null
) : IRequest<GetTalentsNotAppliedResponseDto>;
