using MediatR;
using Shared.Dtos.Jobs;

namespace Modules.Jobs.GetPublicJobs;

public sealed record GetPublicJobsQuery(
    string? SearchTerm = null,
    string? EmploymentType = null,
    string? WorkArrangement = null,
    string? ExperienceLevel = null,
    int Page = 1,
    int PageSize = 12) : IRequest<GetPublicJobsResponseDto>;
