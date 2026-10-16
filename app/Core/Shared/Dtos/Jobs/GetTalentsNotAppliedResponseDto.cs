namespace Shared.Dtos.Jobs;

public sealed class GetTalentsNotAppliedResponseDto
{
    public IReadOnlyList<TalentSummaryDto> Talents { get; init; } = [];
    public int CurrentPage { get; init; }
    public int TotalPages { get; init; }
    public int TotalCount { get; init; }
}

public sealed class TalentSummaryDto
{
    public Guid TalentId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Tagline { get; init; } = string.Empty;
    public string LinkedInUrl { get; init; } = string.Empty;
    public int YearsOfExperience { get; init; }
    public int? AiScore { get; init; }
    public bool InvitationSent { get; init; }
}
