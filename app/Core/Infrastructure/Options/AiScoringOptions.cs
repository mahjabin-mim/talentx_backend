namespace Infrastructure.Options;

public sealed class AiScoringOptions
{
    public const string SectionName = "AiScoring";

    public bool Enabled { get; init; } = true;
    public int WorkerIntervalSeconds { get; init; } = 60;
    public int ProcessingBatchSize { get; init; } = 10;
    public int StaleScanBatchSize { get; init; } = 100;
    public decimal HeuristicWeight { get; init; } = 0.65m;
    public decimal AiWeight { get; init; } = 0.35m;
    public decimal InvitationMatchThreshold { get; init; } = 70m;
    public bool UseHeuristicFallbackWhenAiUnavailable { get; init; } = true;
}
