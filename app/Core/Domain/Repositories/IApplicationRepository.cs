using Domain.Entities;

namespace Domain.Repositories;

public interface IApplicationRepository : IRepository<Application>
{
    Task<bool> IsJobAvailableAsync(
        Guid jobId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);
}