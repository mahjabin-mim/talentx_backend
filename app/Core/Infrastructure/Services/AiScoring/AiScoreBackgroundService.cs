using Infrastructure.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services.AiScoring;

public sealed class AiScoreBackgroundService(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<AiScoringOptions> options,
    ILogger<AiScoreBackgroundService> logger) : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
    private readonly AiScoringOptions _options = options.Value;
    private readonly ILogger<AiScoreBackgroundService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("AI scoring background service is disabled.");
            return;
        }

        var interval = TimeSpan.FromSeconds(Math.Max(15, _options.WorkerIntervalSeconds));
        using var timer = new PeriodicTimer(interval);

        await RunCycleAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunCycleAsync(stoppingToken);
        }
    }

    private async Task RunCycleAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var invalidationService = scope.ServiceProvider.GetRequiredService<IAiScoreInvalidationService>();
            var calculationService = scope.ServiceProvider.GetRequiredService<IAiScoreCalculationService>();

            await invalidationService.EnqueueMissingOrOutdatedScoresAsync(
                _options.StaleScanBatchSize,
                cancellationToken);

            await calculationService.ProcessPendingScoresAsync(
                _options.ProcessingBatchSize,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI scoring background service cycle failed.");
        }
    }
}
