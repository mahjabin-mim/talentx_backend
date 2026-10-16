using Shared.Dtos.Applications;

namespace Infrastructure.Services;

public interface IResumeExtractionService
{
    Task<ExtractApplicationResumeDto> ExtractAsync(
        Stream fileStream,
        string fileName,
        string? contentType,
        CancellationToken cancellationToken = default);
}
