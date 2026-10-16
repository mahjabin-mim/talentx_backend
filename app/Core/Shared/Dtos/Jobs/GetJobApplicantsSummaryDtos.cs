namespace Shared.Dtos.Jobs;

public sealed class GetJobApplicantsSummaryResponseDto
{
    public IReadOnlyList<JobApplicantSummaryDto> Applicants { get; init; } = [];
    public int CurrentPage { get; init; }
    public int TotalPages { get; init; }
    public int TotalCount { get; init; }
}

public sealed class JobApplicantSummaryDto
{
    public Guid ApplicationId { get; init; }
    public Guid TalentId { get; init; }
    public string Name { get; init; } = string.Empty;
    public int? AiProfileMatchScore { get; init; }
    public string Status { get; init; } = string.Empty;
}

public sealed class ApplicantResumeBlobDto
{
    public string ResumeBlob { get; init; } = string.Empty; // Base64
    public string ResumeContentType { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
}
