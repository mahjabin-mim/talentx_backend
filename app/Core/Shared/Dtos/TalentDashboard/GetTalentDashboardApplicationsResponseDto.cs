namespace Shared.Dtos.TalentDashboard;

public sealed class TalentDashboardAppliedJobDto
{
    public Guid Id { get; init; }
    public string JobTitle { get; init; } = string.Empty;
    public string Organization { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string JobLink { get; init; } = string.Empty;
    public DateTimeOffset AppliedDate { get; init; }
    public string SalaryRange { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public decimal? AiScoring { get; init; }
}

public sealed class TalentDashboardInvitationDto
{
    public Guid Id { get; init; }
    public string JobTitle { get; init; } = string.Empty;
    public string Organization { get; init; } = string.Empty;
    public DateTimeOffset InvitedAt { get; init; }
    public string Status { get; init; } = string.Empty;
    public string JobLink { get; init; } = string.Empty;
    public string EmploymentType { get; init; } = string.Empty;
}

public sealed class GetTalentDashboardApplicationsResponseDto
{
    public List<TalentDashboardAppliedJobDto> Applications { get; init; } = [];
    public List<TalentDashboardInvitationDto> Invitations { get; init; } = [];
    public TalentDashboardPaginationDto ApplicationsPagination { get; init; } = new();
    public TalentDashboardPaginationDto InvitationsPagination { get; init; } = new();
    public TalentDashboardSummaryDto Summary { get; init; } = new();
}

public sealed class TalentDashboardPaginationDto
{
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalItems { get; init; }
    public int TotalPages { get; init; }
}

public sealed class TalentDashboardSummaryDto
{
    public int TotalApplied { get; init; }
    public int UnderReviewCount { get; init; }
    public int InterviewCount { get; init; }
    public int TotalInvitations { get; init; }
    public int RespondedCount { get; init; }
    public int PendingCount { get; init; }
}
