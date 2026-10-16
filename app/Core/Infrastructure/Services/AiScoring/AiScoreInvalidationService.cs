using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.AiScoring;

public sealed class AiScoreInvalidationService(
    AppDbContext dbContext,
    ILogger<AiScoreInvalidationService> logger) : IAiScoreInvalidationService
{
    private readonly AppDbContext _dbContext = dbContext;
    private readonly ILogger<AiScoreInvalidationService> _logger = logger;

    public async Task EnqueueScoresForJobAsync(
        Guid jobId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var talentIds = await _dbContext.TalentProfiles
            .AsNoTracking()
            .Where(x => x.User.IsActive && !x.User.Deleted)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var requestedPairs = talentIds.Select(talentId => (JobId: jobId, TalentId: talentId));
        await EnqueuePairsAsync(requestedPairs, reason, cancellationToken);
    }

    public async Task EnqueueScoresForTalentAsync(
        Guid talentId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var jobIds = await _dbContext.Jobs
            .AsNoTracking()
            .Where(x => !x.Deleted)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var requestedPairs = jobIds.Select(jobId => (JobId: jobId, TalentId: talentId));
        await EnqueuePairsAsync(requestedPairs, reason, cancellationToken);
    }

    public async Task EnqueueScoreForPairAsync(
        Guid jobId,
        Guid talentId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        await EnqueuePairsAsync([(jobId, talentId)], reason, cancellationToken);
    }

    public async Task EnqueueMissingOrOutdatedScoresAsync(
        int maxPairs,
        CancellationToken cancellationToken = default)
    {
        if (maxPairs <= 0)
        {
            return;
        }

        var jobs = await _dbContext.Jobs
            .AsNoTracking()
            .Where(x => !x.Deleted)
            .OrderByDescending(x => x.LastModifiedOn ?? x.CreatedOn)
            .Select(x => new JobSnapshot(
                x.Id,
                x.LastModifiedOn ?? x.CreatedOn))
            .ToListAsync(cancellationToken);

        var talents = await _dbContext.TalentProfiles
            .AsNoTracking()
            .Where(x => x.User.IsActive && !x.User.Deleted)
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .Select(x => new TalentSnapshot(
                x.Id,
                x.UpdatedAt ?? x.CreatedAt))
            .ToListAsync(cancellationToken);

        if (jobs.Count == 0 || talents.Count == 0)
        {
            return;
        }

        var jobIds = jobs.Select(x => x.Id).ToList();
        var talentIds = talents.Select(x => x.Id).ToList();

        var scores = await _dbContext.AiScores
            .AsNoTracking()
            .Where(x => jobIds.Contains(x.JobId) && talentIds.Contains(x.TalentId))
            .ToDictionaryAsync(
                x => (x.JobId, x.TalentId),
                x => x.CalculatedAt,
                cancellationToken);

        var latestApplications = await _dbContext.Applications
            .AsNoTracking()
            .Where(x => jobIds.Contains(x.JobId) && talentIds.Contains(x.TalentId))
            .GroupBy(x => new { x.JobId, x.TalentId })
            .Select(x => new
            {
                x.Key.JobId,
                x.Key.TalentId,
                LatestAt = x.Max(item => item.UpdatedAt ?? item.CreatedAt)
            })
            .ToDictionaryAsync(
                x => (x.JobId, x.TalentId),
                x => x.LatestAt,
                cancellationToken);

        var pairsToQueue = new List<(Guid JobId, Guid TalentId)>(maxPairs);

        foreach (var job in jobs)
        {
            foreach (var talent in talents)
            {
                var pairKey = (job.Id, talent.Id);
                var freshestInput = MaxTimestamp(
                    job.ChangedAt,
                    talent.ChangedAt,
                    latestApplications.GetValueOrDefault(pairKey));

                if (!scores.TryGetValue(pairKey, out var calculatedAt) || calculatedAt < freshestInput)
                {
                    pairsToQueue.Add(pairKey);

                    if (pairsToQueue.Count >= maxPairs)
                    {
                        await EnqueuePairsAsync(pairsToQueue, "missing-or-outdated", cancellationToken);
                        return;
                    }
                }
            }
        }

        if (pairsToQueue.Count > 0)
        {
            await EnqueuePairsAsync(pairsToQueue, "missing-or-outdated", cancellationToken);
        }
    }

    private async Task EnqueuePairsAsync(
        IEnumerable<(Guid JobId, Guid TalentId)> pairs,
        string reason,
        CancellationToken cancellationToken)
    {
        var distinctPairs = pairs
            .Where(x => x.JobId != Guid.Empty && x.TalentId != Guid.Empty)
            .Distinct()
            .ToList();

        if (distinctPairs.Count == 0)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var jobIds = distinctPairs.Select(x => x.JobId).Distinct().ToList();
        var talentIds = distinctPairs.Select(x => x.TalentId).Distinct().ToList();

        var existingRows = await _dbContext.AiScoreRefreshQueues
            .Where(x => jobIds.Contains(x.JobId) && talentIds.Contains(x.TalentId))
            .ToListAsync(cancellationToken);

        var existingLookup = existingRows.ToDictionary(x => (x.JobId, x.TalentId));

        foreach (var pair in distinctPairs)
        {
            if (existingLookup.TryGetValue(pair, out var existingRow))
            {
                existingRow.RequestedAt = now;
                existingRow.Reason = MergeReason(existingRow.Reason, reason);
                existingRow.LastError = null;
                continue;
            }

            _dbContext.AiScoreRefreshQueues.Add(new AiScoreRefreshQueue
            {
                JobId = pair.JobId,
                TalentId = pair.TalentId,
                Reason = reason,
                RequestedAt = now
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Queued {Count} AI score refresh pair(s). Reason: {Reason}",
            distinctPairs.Count,
            reason);
    }

    private static string MergeReason(string existingReason, string newReason)
    {
        if (string.IsNullOrWhiteSpace(existingReason))
        {
            return newReason;
        }

        if (existingReason.Contains(newReason, StringComparison.OrdinalIgnoreCase))
        {
            return existingReason;
        }

        var merged = $"{existingReason},{newReason}";
        return merged.Length <= 200 ? merged : merged[..200];
    }

    private static DateTimeOffset MaxTimestamp(
        DateTimeOffset first,
        DateTimeOffset second,
        DateTimeOffset? third)
    {
        var max = first > second ? first : second;

        if (third.HasValue && third.Value > max)
        {
            max = third.Value;
        }

        return max;
    }

    private sealed record JobSnapshot(Guid Id, DateTimeOffset ChangedAt);
    private sealed record TalentSnapshot(Guid Id, DateTimeOffset ChangedAt);
}
