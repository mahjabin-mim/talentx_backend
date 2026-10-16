namespace Domain.Entities;

public sealed class AiScoreRefreshQueue
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public Guid TalentId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset RequestedAt { get; set; }
    public DateTimeOffset? LastAttemptedAt { get; set; }
    public int AttemptCount { get; set; }
    public string? LastError { get; set; }
}
