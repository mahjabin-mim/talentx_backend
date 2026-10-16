namespace Infrastructure.Services;

public sealed class ResumeUploadResult
{
    public string ObjectKey { get; init; } = string.Empty;

    public string ObjectUrl { get; init; } = string.Empty;
}