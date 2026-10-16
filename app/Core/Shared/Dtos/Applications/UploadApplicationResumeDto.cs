using Microsoft.AspNetCore.Http;

namespace Shared.Dtos.Applications;

public sealed class UploadApplicationResumeDto
{
    public const long MaxFileSizeBytes = 2 * 1024 * 1024;

    public static readonly string[] AllowedExtensions = [".pdf"];

    public IFormFile Resume { get; init; } = default!;
}
