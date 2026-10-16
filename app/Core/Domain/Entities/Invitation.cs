using Domain.Enums;

namespace Domain.Entities;

public sealed class Invitation
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public Guid TalentId { get; set; }
    public InvitationStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Job Job { get; set; } = null!;
    public TalentProfile Talent { get; set; } = null!;
}
