namespace Shared.Dtos.Jobs;

public class JobWriteRequestDto
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Responsibilities { get; init; } = string.Empty;
    public IReadOnlyList<string> Skills { get; init; } = [];
    public string EmploymentType { get; init; } = string.Empty;
    public string WorkArrangement { get; init; } = string.Empty;
    public string ExperienceLevel { get; init; } = string.Empty;
    public int ExperienceMinYears { get; init; }
    public int ExperienceMaxYears { get; init; }
    public decimal SalaryMin { get; init; }
    public decimal SalaryMax { get; init; }
    public string? CountryCode { get; init; }
    public string? City { get; init; }
    public string? Timezone { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
    public int NumberOfPositions { get; init; }
    public DateTimeOffset ApplicationDeadline { get; init; }
}

public sealed class GenerateJobDescriptionRequestDto : JobWriteRequestDto
{
    public Guid? JobId { get; init; }
}

public sealed class CreateJobRequestDto : JobWriteRequestDto
{
    public bool PublishJob { get; init; }
}

public sealed class UpdateJobRequestDto : JobWriteRequestDto
{
    public bool PublishJob { get; init; }
}
