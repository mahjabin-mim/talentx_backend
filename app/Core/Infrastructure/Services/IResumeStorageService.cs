namespace Infrastructure.Services;

public interface IResumeStorageService
{
    Task<ResumeUploadResult> UploadAsync(
        Stream fileStream,
        string fileName,
        string? contentType,
        CancellationToken cancellationToken = default,
        string? folderName = null);

    Task<ResumeDownloadResult> DownloadByUrlAsync(
        string objectUrl,
        CancellationToken cancellationToken = default);
}
