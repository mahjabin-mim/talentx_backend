using Domain.Entities;
using Domain.Enums;
using Shared.Dtos.TalentProfiles;

namespace Modules.TalentProfiles;

internal static class TalentProfileMappings
{
    private static readonly IReadOnlyDictionary<string, TalentSkillLanguage> SkillLanguageByLabel =
        new Dictionary<string, TalentSkillLanguage>(StringComparer.OrdinalIgnoreCase)
        {
            ["JavaScript"] = TalentSkillLanguage.JavaScript,
            ["TypeScript"] = TalentSkillLanguage.TypeScript,
            ["Python"] = TalentSkillLanguage.Python,
            ["Java"] = TalentSkillLanguage.Java,
            ["C#"] = TalentSkillLanguage.CSharp,
            ["C++"] = TalentSkillLanguage.CPlusPlus,
            ["Go"] = TalentSkillLanguage.Go,
            ["PHP"] = TalentSkillLanguage.Php,
            ["Ruby"] = TalentSkillLanguage.Ruby,
            ["Swift"] = TalentSkillLanguage.Swift,
            ["Kotlin"] = TalentSkillLanguage.Kotlin,
            ["Dart"] = TalentSkillLanguage.Dart,
            ["Rust"] = TalentSkillLanguage.Rust,
            ["SQL"] = TalentSkillLanguage.Sql
        };

    private static readonly IReadOnlyDictionary<TalentSkillLanguage, string> SkillLabelByLanguage =
        SkillLanguageByLabel.ToDictionary(x => x.Value, x => x.Key);

    internal static TalentProfileDto MapToDto(User user, TalentProfile? profile)
    {
        return new TalentProfileDto
        {
            Name = user.Fullname ?? string.Empty,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            ResumeUrl = profile?.ResumeUrl ?? string.Empty,
            ResumeFileName = profile?.ResumeFileName ?? string.Empty,
            PictureUrl = profile?.PictureUrl ?? string.Empty,
            PictureFileName = profile?.PictureFileName ?? string.Empty,
            Location = profile?.Location ?? string.Empty,
            Tagline = profile?.Tagline ?? string.Empty,
            Bio = profile?.Bio ?? string.Empty,
            Linkedin = profile?.Linkedin ?? string.Empty,
            AdditionalUrl = profile?.AdditionalUrl ?? string.Empty,
            VideoIntro = profile?.VideoIntro ?? string.Empty,
            Experience = profile?.Experience ?? 0,
            Workplaces = profile?.Workplaces ?? string.Empty,
            ExpectedSalary = profile?.ExpectedSalary ?? 0,
            Skills = profile?.Skills
                .OrderBy(x => x.DisplayOrder)
                .Select(MapSkillToDto)
                .ToList() ?? []
        };
    }

    internal static bool IsSupportedSkillLanguage(string? value)
    {
        return TryParseSkillLanguage(value, out _);
    }

    internal static bool TryParseSkillLanguage(string? value, out TalentSkillLanguage language)
    {
        if (!string.IsNullOrWhiteSpace(value)
            && SkillLanguageByLabel.TryGetValue(value.Trim(), out language))
        {
            return true;
        }

        language = default;
        return false;
    }

    internal static string ToSkillLanguageLabel(TalentSkillLanguage language)
    {
        return SkillLabelByLanguage.TryGetValue(language, out var label)
            ? label
            : throw new ArgumentOutOfRangeException(nameof(language), language, "Unsupported skill language.");
    }

    private static TalentProfileSkillDto MapSkillToDto(TalentProfileSkill skill)
    {
        return new TalentProfileSkillDto
        {
            Language = ToSkillLanguageLabel(skill.Language),
            Experience = skill.YearsOfExperience
        };
    }
}
