namespace Shared.Dtos.TalentProfiles;

public sealed class TalentProfileTextInfoResponseDto
{
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public string Tagline { get; init; } = string.Empty;
    public string Bio { get; init; } = string.Empty;
    public string Linkedin { get; init; } = string.Empty;
    public string AdditionalUrl { get; init; } = string.Empty;
    public string VideoIntro { get; init; } = string.Empty;
    public string Workplaces { get; init; } = string.Empty;
    public List<string> Skills { get; init; } = [];
}
