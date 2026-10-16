using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class TalentProfileRepository(AppDbContext dbContext)
    : Repository<TalentProfile>(dbContext), ITalentProfileRepository
{
    public async Task<TalentProfile?> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await DbContext.TalentProfiles
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
    }
}