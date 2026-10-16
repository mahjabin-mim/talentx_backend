namespace Shared.Dtos.Jobs;

public sealed class GetPublicJobsResponseDto
{
    public IReadOnlyList<PublicJobCardDto> Jobs { get; init; } = [];
    public int CurrentPage { get; init; }
    public int TotalPages { get; init; }
    public int TotalCount { get; init; }
}

public sealed class PublicJobCardDto
{
    public Guid JobId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string CompanyName { get; init; } = string.Empty;
    public string EmploymentType { get; init; } = string.Empty;
    public string WorkArrangement { get; init; } = string.Empty;
    public string ExperienceLevel { get; init; } = string.Empty;
    public int ExperienceMinYears { get; init; }
    public int ExperienceMaxYears { get; init; }
    public decimal SalaryMin { get; init; }
    public decimal SalaryMax { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
    public int NumberOfPositions { get; init; }
    public string Location { get; init; } = string.Empty;
    public DateTimeOffset ApplicationDeadline { get; init; }
    public DateTimeOffset CreatedOn { get; init; }
    public string Status { get; init; } = string.Empty;
}
