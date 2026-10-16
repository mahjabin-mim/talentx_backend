namespace Shared.Dtos.TalentProfiles;

public sealed class UpsertTalentProfileSkillRequestDto
{
    public string Language { get; init; } = string.Empty;

    public decimal Experience { get; init; }
}
