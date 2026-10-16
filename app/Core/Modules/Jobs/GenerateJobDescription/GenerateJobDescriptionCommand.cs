using MediatR;
using Shared.Dtos.Jobs;

namespace Modules.Jobs.GenerateJobDescription;

public sealed record GenerateJobDescriptionCommand(
    Guid UserId,
    Guid? JobId,
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
    DateTimeOffset ApplicationDeadline) : IRequest<GenerateJobDescriptionResponseDto>;
