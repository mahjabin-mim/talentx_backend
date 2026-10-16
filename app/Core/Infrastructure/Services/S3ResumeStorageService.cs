using Amazon.S3;
using Amazon.S3.Model;
using Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public sealed class S3ResumeStorageService(
    IAmazonS3 s3Client,
    IOptions<S3Options> s3Options,
    ILogger<S3ResumeStorageService> logger) : IResumeStorageService
{
    private readonly IAmazonS3 _s3Client = s3Client;
    private readonly S3Options _s3Options = s3Options.Value;
    private readonly ILogger<S3ResumeStorageService> _logger = logger;

    public async Task<ResumeUploadResult> UploadAsync(
        Stream fileStream,
        string fileName,
        string? contentType,
        CancellationToken cancellationToken = default,
        string? folderName = null)
    {
        ArgumentNullException.ThrowIfNull(fileStream);

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name is required.", nameof(fileName));
        }

        var bucketName = _s3Options.BucketName?.Trim();
        if (string.IsNullOrWhiteSpace(bucketName))
        {
            throw new InvalidOperationException("S3 bucket is not configured.");
        }

        if (fileStream.CanSeek)
        {
            fileStream.Position = 0;
        }

        var objectKey = BuildObjectKey(fileName, folderName);

        var request = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = objectKey,
            InputStream = fileStream,
            ContentType = string.IsNullOrWhiteSpace(contentType)
                ? "application/octet-stream"
                : contentType.Trim(),
            AutoCloseStream = false
        };

        await _s3Client.PutObjectAsync(request, cancellationToken);

        var objectUrl = BuildObjectUrl(bucketName, objectKey);

        _logger.LogInformation(
            "File uploaded to S3. Bucket: {BucketName}, Key: {ObjectKey}",
            bucketName,
            objectKey);

        return new ResumeUploadResult
        {
            ObjectKey = objectKey,
            ObjectUrl = objectUrl
        };
    }

    private string BuildObjectKey(string fileName, string? folderName)
    {
        var configuredFolder = string.IsNullOrWhiteSpace(folderName)
            ? _s3Options.ResumeFolder
            : folderName;

        var folder = string.IsNullOrWhiteSpace(configuredFolder)
            ? "resumes"
            : configuredFolder.Trim().Trim('/');

        var extension = Path.GetExtension(fileName);
        var normalizedExtension = string.IsNullOrWhiteSpace(extension)
            ? ".bin"
            : extension.ToLowerInvariant();

        return $"{folder}/{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}{normalizedExtension}";
    }

    private string BuildObjectUrl(string bucketName, string objectKey)
    {
        var publicBaseUrl = _s3Options.PublicBaseUrl?.Trim();
        if (!string.IsNullOrWhiteSpace(publicBaseUrl))
        {
            return $"{publicBaseUrl.TrimEnd('/')}/{objectKey}";
        }

        var region = _s3Client.Config.RegionEndpoint?.SystemName;
        if (string.IsNullOrWhiteSpace(region))
        {
            return $"https://{bucketName}.s3.amazonaws.com/{objectKey}";
        }

        return $"https://{bucketName}.s3.{region}.amazonaws.com/{objectKey}";
    }

    public async Task<ResumeDownloadResult> DownloadByUrlAsync(
        string objectUrl,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(objectUrl))
        {
            throw new ArgumentException("Object URL is required.", nameof(objectUrl));
        }

        var bucketName = _s3Options.BucketName?.Trim();
        if (string.IsNullOrWhiteSpace(bucketName))
        {
            throw new InvalidOperationException("S3 bucket is not configured.");
        }

        var objectKey = ExtractObjectKeyFromUrl(objectUrl);

        var response = await _s3Client.GetObjectAsync(new GetObjectRequest
        {
            BucketName = bucketName,
            Key = objectKey
        }, cancellationToken);

        return new ResumeDownloadResult
        {
            Stream = response.ResponseStream,
            ContentType = string.IsNullOrWhiteSpace(response.Headers.ContentType)
                ? "application/octet-stream"
                : response.Headers.ContentType
        };
    }

    private static string ExtractObjectKeyFromUrl(string objectUrl)
    {
        if (!Uri.TryCreate(objectUrl, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException("Invalid resume URL.");
        }

        var key = uri.AbsolutePath.TrimStart('/');
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException("Resume object key could not be resolved.");
        }

        return key;
    }
}
