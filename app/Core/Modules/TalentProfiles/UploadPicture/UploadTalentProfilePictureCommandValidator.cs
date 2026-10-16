using FluentValidation;
using Shared.Dtos.TalentProfiles;

namespace Modules.TalentProfiles.UploadPicture;

public sealed class UploadTalentProfilePictureCommandValidator : AbstractValidator<UploadTalentProfilePictureCommand>
{
    public UploadTalentProfilePictureCommandValidator()
    {
        RuleFor(x => x.Picture)
            .NotNull()
            .Must(file => file.Length > 0)
            .WithMessage("Profile picture is required.")
            .Must(file => file.Length <= UploadTalentProfilePictureRequestDto.MaxFileSizeBytes)
            .WithMessage($"Profile picture size must be {UploadTalentProfilePictureRequestDto.MaxFileSizeBytes / (1024 * 1024)}MB or less.")
            .Must(file =>
            {
                var extension = Path.GetExtension(file.FileName);
                return UploadTalentProfilePictureRequestDto.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
            })
            .WithMessage("Only JPG, JPEG, PNG, and WEBP profile pictures are supported.");
    }
}
