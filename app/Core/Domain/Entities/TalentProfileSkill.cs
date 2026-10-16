using Domain.Enums;

namespace Domain.Entities;

public sealed class TalentProfileSkill
{
    public Guid Id { get; set; }

    public Guid TalentProfileId { get; set; }

    public TalentSkillLanguage Language { get; set; }

    public decimal YearsOfExperience { get; set; }

    public int DisplayOrder { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public TalentProfile TalentProfile { get; set; } = null!;
}
