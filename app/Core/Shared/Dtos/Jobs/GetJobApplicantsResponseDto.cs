namespace Shared.Dtos.Jobs;


public sealed class GetJobApplicantsResponseDto
{
    public IReadOnlyList<JobApplicantDto> Applicants { get; init; } = [];
    public int CurrentPage { get; init; }
    public int TotalPages { get; init; }
    public int TotalCount { get; init; }
}

public sealed class JobApplicantDto
{
    public string Name { get; init; } = string.Empty;
    public string ResumeUrl { get; init; } = string.Empty;
    public int? AiProfileMatchScore { get; init; }
    public string Status { get; init; } = string.Empty;
}
