using Ardalis.Specification;
using Domain.Entities;

namespace Domain.Specifications;

public sealed class TalentDashboardApplicationsByUserIdSpec : Specification<Application>
{
    public TalentDashboardApplicationsByUserIdSpec(Guid userId)
    {
        Query.AsNoTracking();
        Query.Where(x => x.Talent.UserId == userId);
        Query.Include(x => x.Job)
            .ThenInclude(x => x.Employer);
        Query.Include(x => x.Talent);
        Query.OrderByDescending(x => x.CreatedAt);
    }
}
