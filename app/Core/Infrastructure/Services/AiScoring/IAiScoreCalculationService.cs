namespace Infrastructure.Services.AiScoring;

public interface IAiScoreCalculationService
{
    Task ProcessPendingScoresAsync(int maxBatchSize, CancellationToken cancellationToken = default);
}
