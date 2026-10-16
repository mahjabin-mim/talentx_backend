namespace Shared.Dtos.TalentProfiles;

public sealed class TalentProfileDto
{
    public string Name { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string PhoneNumber { get; init; } = string.Empty;

    public string ResumeUrl { get; init; } = string.Empty;

    public string ResumeFileName { get; init; } = string.Empty;

    public string PictureUrl { get; init; } = string.Empty;

    public string PictureFileName { get; init; } = string.Empty;

    public string Location { get; init; } = string.Empty;

    public string Tagline { get; init; } = string.Empty;

    public string Bio { get; init; } = string.Empty;

    public string Linkedin { get; init; } = string.Empty;

    public string AdditionalUrl { get; init; } = string.Empty;

    public string VideoIntro { get; init; } = string.Empty;

    public int Experience { get; init; }

    public string Workplaces { get; init; } = string.Empty;

    public decimal ExpectedSalary { get; init; }

    public List<TalentProfileSkillDto> Skills { get; init; } = [];
}
