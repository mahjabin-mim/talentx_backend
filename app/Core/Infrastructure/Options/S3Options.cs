namespace Infrastructure.Options;

public sealed class S3Options
{
    public const string SectionName = "S3";

    public string BucketName { get; init; } = string.Empty;

    public string PublicBaseUrl { get; init; } = string.Empty;

    public string ResumeFolder { get; init; } = "resumes";

    public string ProfilePictureFolder { get; init; } = "profile-pictures";
}
