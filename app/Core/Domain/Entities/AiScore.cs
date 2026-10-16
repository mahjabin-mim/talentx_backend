namespace Domain.Entities;

public sealed class AiScore
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public Guid TalentId { get; set; }
    public decimal Score { get; set; }
    public DateTimeOffset CalculatedAt { get; set; }

    public Job Job { get; set; } = null!;
    public TalentProfile Talent { get; set; } = null!;
}
