using Domain.Enums;
using FluentValidation;

namespace Modules.Jobs.UpdateJob;

public sealed class UpdateJobCommandValidator : AbstractValidator<UpdateJobCommand>
{
    public UpdateJobCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty);

        RuleFor(x => x.JobId)
            .NotEqual(Guid.Empty);

        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(160);

        RuleFor(x => x.Description)
            .MaximumLength(4000);

        RuleFor(x => x.Description)
            .NotEmpty()
            .When(x => x.PublishJob);

        RuleFor(x => x.Responsibilities)
            .MaximumLength(4000);

        RuleFor(x => x.Responsibilities)
            .NotEmpty()
            .When(x => x.PublishJob);

        RuleFor(x => x.Skills)
            .NotNull()
            .Must(x => x is not null && x.Count <= 50)
            .WithMessage("A maximum of 50 skills is allowed.")
            .Must(HaveUniqueSkills)
            .WithMessage("Skills must be unique.");

        RuleFor(x => x.Skills)
            .NotEmpty()
            .When(x => x.PublishJob);

        RuleForEach(x => x.Skills)
            .NotEmpty()
            .MaximumLength(120);

        RuleFor(x => x.EmploymentType)
            .NotEmpty()
            .Must(BeEmploymentType)
            .WithMessage("Unsupported employment type.");

        RuleFor(x => x.WorkArrangement)
            .NotEmpty()
            .Must(BeWorkArrangement)
            .WithMessage("Unsupported work arrangement.");

        RuleFor(x => x.ExperienceLevel)
            .NotEmpty()
            .Must(BeExperienceLevel)
            .WithMessage("Unsupported experience level.");

        RuleFor(x => x.ExperienceMinYears)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(80);

        RuleFor(x => x.ExperienceMaxYears)
            .GreaterThanOrEqualTo(x => x.ExperienceMinYears)
            .LessThanOrEqualTo(80);

        RuleFor(x => x.SalaryMin)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.SalaryMax)
            .GreaterThanOrEqualTo(x => x.SalaryMin);

        RuleFor(x => x.CurrencyCode)
            .NotEmpty()
            .MaximumLength(3);

        RuleFor(x => x.NumberOfPositions)
            .GreaterThan(0)
            .LessThanOrEqualTo(1000);

        RuleFor(x => x.ApplicationDeadline)
            .Must(x => x > DateTimeOffset.UtcNow)
            .WithMessage("Application deadline must be in the future.");

        RuleFor(x => x.Timezone)
            .NotEmpty()
            .MaximumLength(64)
            .When(x => ParseWorkArrangement(x.WorkArrangement) == WorkArrangement.Remote);

        RuleFor(x => x.CountryCode)
            .NotEmpty()
            .MaximumLength(3)
            .When(x => ParseWorkArrangement(x.WorkArrangement) != WorkArrangement.Remote);

        RuleFor(x => x.City)
            .NotEmpty()
            .MaximumLength(120)
            .When(x => ParseWorkArrangement(x.WorkArrangement) != WorkArrangement.Remote);

        RuleFor(x => x.CountryCode)
            .MaximumLength(3)
            .When(x => !string.IsNullOrWhiteSpace(x.CountryCode));

        RuleFor(x => x.City)
            .MaximumLength(120)
            .When(x => !string.IsNullOrWhiteSpace(x.City));

        RuleFor(x => x.Timezone)
            .MaximumLength(64)
            .When(x => !string.IsNullOrWhiteSpace(x.Timezone));
    }

    private static bool BeEmploymentType(string value)
        => Enum.TryParse<EmploymentType>(value, true, out _);

    private static bool BeExperienceLevel(string value)
        => Enum.TryParse<ExperienceLevel>(value, true, out _);

    private static bool BeWorkArrangement(string value)
        => Enum.TryParse<WorkArrangement>(value, true, out _);

    private static WorkArrangement ParseWorkArrangement(string value)
        => Enum.TryParse<WorkArrangement>(value, true, out var parsed)
            ? parsed
            : WorkArrangement.Remote;

    private static bool HaveUniqueSkills(IReadOnlyList<string>? skills)
    {
        if (skills is null)
        {
            return false;
        }

        var normalizedSkills = skills
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .ToArray();

        return normalizedSkills.Distinct(StringComparer.OrdinalIgnoreCase).Count() == normalizedSkills.Length;
    }
}
