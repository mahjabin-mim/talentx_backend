using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class EmployerRepository(AppDbContext dbContext) : IEmployerRepository
{
    private readonly AppDbContext _dbContext = dbContext;

    public async Task<Employer?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Employers
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
    }

    public void Add(Employer employer)
    {
        _dbContext.Employers.Add(employer);
    }
}