namespace Infrastructure.Services.AiScoring;

public interface IAiScoreInvalidationService
{
    Task EnqueueScoresForJobAsync(
        Guid jobId,
        string reason,
        CancellationToken cancellationToken = default);

    Task EnqueueScoresForTalentAsync(
        Guid talentId,
        string reason,
        CancellationToken cancellationToken = default);

    Task EnqueueScoreForPairAsync(
        Guid jobId,
        Guid talentId,
        string reason,
        CancellationToken cancellationToken = default);

    Task EnqueueMissingOrOutdatedScoresAsync(
        int maxPairs,
        CancellationToken cancellationToken = default);
}
