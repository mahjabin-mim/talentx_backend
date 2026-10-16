using Shared.Dtos.Jobs;

namespace Infrastructure.Services;

public interface IJobDescriptionGenerationService
{
    Task<string> GenerateAsync(
        GenerateJobDescriptionRequestDto request,
        CancellationToken cancellationToken = default);
}
