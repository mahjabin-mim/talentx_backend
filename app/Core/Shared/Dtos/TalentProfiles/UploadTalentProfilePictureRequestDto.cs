using Microsoft.AspNetCore.Http;

namespace Shared.Dtos.TalentProfiles;

public sealed class UploadTalentProfilePictureRequestDto
{
    public const long MaxFileSizeBytes = 5 * 1024 * 1024;

    public static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    public IFormFile Picture { get; init; } = default!;
}
