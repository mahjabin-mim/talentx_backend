using FluentValidation;
using Modules.TalentProfiles;
using Shared.Dtos.TalentProfiles;

namespace Modules.TalentProfiles.Upsert;

public sealed class UpsertTalentProfileCommandValidator : AbstractValidator<UpsertTalentProfileCommand>
{
    public UpsertTalentProfileCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(120);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(254);

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20)
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));

        RuleFor(x => x.Location)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.Location));

        RuleFor(x => x.Tagline)
            .MaximumLength(150)
            .When(x => !string.IsNullOrWhiteSpace(x.Tagline));

        RuleFor(x => x.Bio)
            .MaximumLength(2000)
            .When(x => !string.IsNullOrWhiteSpace(x.Bio));

        RuleFor(x => x.Linkedin)
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("'Linkedin' is not a valid URL.")
            .When(x => !string.IsNullOrWhiteSpace(x.Linkedin));

        RuleFor(x => x.Linkedin)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.Linkedin));

        RuleFor(x => x.AdditionalUrl)
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("'Additional Url' is not a valid URL.")
            .When(x => !string.IsNullOrWhiteSpace(x.AdditionalUrl));

        RuleFor(x => x.AdditionalUrl)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.AdditionalUrl));

        RuleFor(x => x.VideoIntro)
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("'Video Intro' is not a valid URL.")
            .When(x => !string.IsNullOrWhiteSpace(x.VideoIntro));

        RuleFor(x => x.VideoIntro)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.VideoIntro));

        RuleFor(x => x.Experience)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.Workplaces)
            .MaximumLength(2000)
            .When(x => !string.IsNullOrWhiteSpace(x.Workplaces));

        RuleFor(x => x.ExpectedSalary)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.Skills)
            .Must(skills => skills.Count <= 5)
            .WithMessage("A maximum of 5 skills is allowed.");

        RuleForEach(x => x.Skills)
            .ChildRules(skill =>
            {
                skill.RuleFor(x => x.Language)
                    .NotEmpty()
                    .Must(TalentProfileMappings.IsSupportedSkillLanguage)
                    .WithMessage("Skill language is not supported.");

                skill.RuleFor(x => x.Experience)
                    .GreaterThanOrEqualTo(0);
            });

        RuleFor(x => x.Skills)
            .Must(HaveUniqueSkillLanguages)
            .WithMessage("Duplicate skill languages are not allowed.");
    }

    private static bool HaveUniqueSkillLanguages(IReadOnlyList<UpsertTalentProfileSkillRequestDto> skills)
    {
        var languages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var skill in skills)
        {
            var normalizedLanguage = skill.Language?.Trim() ?? string.Empty;

            if (!languages.Add(normalizedLanguage))
            {
                return false;
            }
        }

        return true;
    }
}
