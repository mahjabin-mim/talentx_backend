using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class ApplicationRepository(AppDbContext dbContext)
    : Repository<Application>(dbContext), IApplicationRepository
{
    public async Task<bool> IsJobAvailableAsync(
        Guid jobId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        return await DbContext.Jobs.AnyAsync(x =>
            x.Id == jobId &&
            !x.Deleted &&
            x.Status == JobStatus.Active &&
            x.ApplicationDeadline > now,
            cancellationToken);
    }

    public override async Task AddAsync(
        Application application,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = DbContext.Database.IsRelational()
            ? await DbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        await base.AddAsync(application, cancellationToken);

        await DbContext.SaveChangesAsync(cancellationToken);

        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }
    }
}
