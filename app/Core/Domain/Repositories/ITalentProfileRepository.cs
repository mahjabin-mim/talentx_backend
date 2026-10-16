using Domain.Entities;

namespace Domain.Repositories;

public interface ITalentProfileRepository : IRepository<TalentProfile>
{
    Task<TalentProfile?> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}