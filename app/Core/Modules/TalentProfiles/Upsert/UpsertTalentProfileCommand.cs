using MediatR;
using Shared.Dtos.TalentProfiles;

namespace Modules.TalentProfiles.Upsert;

public sealed record UpsertTalentProfileCommand(
    Guid UserId,
    string Name,
    string Email,
    string? PhoneNumber,
    string? Location,
    string? Tagline,
    string? Bio,
    string? Linkedin,
    string? AdditionalUrl,
    string? VideoIntro,
    int Experience,
    string? Workplaces,
    decimal ExpectedSalary,
    IReadOnlyList<UpsertTalentProfileSkillRequestDto> Skills) : IRequest<TalentProfileDto>;
