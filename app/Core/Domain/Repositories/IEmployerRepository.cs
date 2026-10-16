using Domain.Entities;

namespace Domain.Repositories;

public interface IEmployerRepository
{
    Task<Employer?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    void Add(Employer employer);
}