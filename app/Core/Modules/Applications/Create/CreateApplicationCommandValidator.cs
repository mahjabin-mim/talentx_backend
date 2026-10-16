using FluentValidation;
using Shared.Dtos.Applications;

namespace Modules.Applications.Create;

public sealed class CreateApplicationCommandValidator : AbstractValidator<CreateApplicationCommand>
{
    public CreateApplicationCommandValidator()
    {
        RuleFor(x => x.JobId)
            .NotEqual(Guid.Empty);

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(255);

        RuleFor(x => x.ContactNumber)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(x => x.Education)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(x => x.Department)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(x => x.CGPA)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.LinkedInUrl)
            .NotEmpty()
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("'Linked In Url' is not a valid URL.")
            .MaximumLength(255);

        RuleFor(x => x.AdditionalUrl)
            .NotEmpty()
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("'Additional Url' is not a valid URL.")
            .MaximumLength(255);

        RuleFor(x => x.Location)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.CoverLetter)
            .NotEmpty();

        RuleFor(x => x.ExpectedSalary)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.Experience)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.Resume)
            .NotNull()
            .Must(file => file.Length > 0)
            .WithMessage("Resume file is required.")
            .Must(file => file.Length <= UploadApplicationResumeDto.MaxFileSizeBytes)
            .WithMessage($"Resume file size must be {UploadApplicationResumeDto.MaxFileSizeBytes / (1024 * 1024)}MB or less.")
            .Must(file =>
            {
                var extension = Path.GetExtension(file.FileName);
                return UploadApplicationResumeDto.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
            })
            .WithMessage("Only PDF resume files are supported.");
    }
}
