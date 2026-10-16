using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using Domain.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class Repository<TEntity>(AppDbContext dbContext)
    : IRepository<TEntity>
    where TEntity : class
{
    protected readonly AppDbContext DbContext = dbContext;
    protected DbSet<TEntity> Set => DbContext.Set<TEntity>();

    public virtual async Task AddAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        await Set.AddAsync(entity, cancellationToken);
    }


    public virtual async Task<bool> AnyAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        return await SpecificationEvaluator.Default
            .GetQuery(Set.AsQueryable(), specification)
            .AnyAsync(cancellationToken);
    }

    public virtual async Task<TEntity?> FirstOrDefaultAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        return await SpecificationEvaluator.Default
            .GetQuery(Set.AsQueryable(), specification)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public virtual async Task<List<TEntity>> ListAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        return await SpecificationEvaluator.Default
            .GetQuery(Set.AsQueryable(), specification)
            .ToListAsync(cancellationToken);
    }

    public virtual void Remove(TEntity entity)
    {
        Set.Remove(entity);
    }
}
