namespace Shared.Dtos.TalentProfiles;

public sealed class UpsertTalentProfileRequestDto
{
    public string Name { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string? PhoneNumber { get; init; }

    public string? Location { get; init; }

    public string? Tagline { get; init; }

    public string? Bio { get; init; }

    public string? Linkedin { get; init; }

    public string? AdditionalUrl { get; init; }

    public string? VideoIntro { get; init; }

    public int Experience { get; init; }

    public string? Workplaces { get; init; }

    public decimal ExpectedSalary { get; init; }

    public List<UpsertTalentProfileSkillRequestDto> Skills { get; init; } = [];
}
