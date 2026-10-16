using FluentValidation;
using Shared.Dtos.TalentProfiles;

namespace Modules.TalentProfiles.UploadResume;

public sealed class UploadTalentProfileResumeCommandValidator : AbstractValidator<UploadTalentProfileResumeCommand>
{
    public UploadTalentProfileResumeCommandValidator()
    {
        RuleFor(x => x.Resume)
            .NotNull()
            .Must(file => file.Length > 0)
            .WithMessage("Resume file is required.")
            .Must(file => file.Length <= UploadTalentProfileResumeRequestDto.MaxFileSizeBytes)
            .WithMessage($"Resume file size must be {UploadTalentProfileResumeRequestDto.MaxFileSizeBytes / (1024 * 1024)}MB or less.")
            .Must(file =>
            {
                var extension = Path.GetExtension(file.FileName);
                return UploadTalentProfileResumeRequestDto.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
            })
            .WithMessage("Only PDF resume files are supported.");
    }
}
