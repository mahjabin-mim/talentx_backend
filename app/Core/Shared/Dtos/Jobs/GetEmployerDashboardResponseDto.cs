namespace Shared.Dtos.Jobs;

public sealed class GetEmployerDashboardResponseDto
{
    public int JobsPosted { get; init; }
    public int ActiveJobs { get; init; }
    public int ClosedJobs { get; init; }
    public int TotalHires { get; init; }
    public IReadOnlyList<string> AvailableStatuses { get; init; } = [];
    public IReadOnlyList<EmployerDashboardJobDto> Jobs { get; init; } = [];

    public int CurrentPage { get; init; }
    public int TotalPages { get; init; }
}

public sealed class EmployerDashboardJobDto
{
    public Guid JobId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTimeOffset CreatedOn { get; init; }
    public int Applicants { get; init; }
}

public sealed class GetJobDetailsResponseDto
{
    public Guid JobId { get; init; }
    public string CompanyName { get; init; } = string.Empty;
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
    public string Status { get; init; } = string.Empty;
}

public sealed class UpdateJobStatusRequestDto
{
    public string Action { get; init; } = string.Empty;
}
