using MediatR;
using Shared.Dtos.Jobs;

namespace Modules.Jobs.CreateJob;

public sealed record CreateJobCommand(
    Guid UserId,
    bool PublishJob,
    string Title,
    string Description,
    string Responsibilities,
    IReadOnlyList<string> Skills,
    string EmploymentType,
    string WorkArrangement,
    string ExperienceLevel,
    int ExperienceMinYears,
    int ExperienceMaxYears,
    decimal SalaryMin,
    decimal SalaryMax,
    string? CountryCode,
    string? City,
    string? Timezone,
    string CurrencyCode,
    int NumberOfPositions,
    DateTimeOffset ApplicationDeadline) : IRequest<CreateJobResponseDto>;
