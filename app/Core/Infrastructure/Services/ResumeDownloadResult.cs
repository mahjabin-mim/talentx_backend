namespace Infrastructure.Services;

public sealed class ResumeDownloadResult
{
    public Stream Stream { get; init; } = Stream.Null;
    public string ContentType { get; init; } = "application/octet-stream";
}
