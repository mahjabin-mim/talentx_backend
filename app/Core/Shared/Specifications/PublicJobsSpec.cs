using Ardalis.Specification;
using Domain.Entities;
using Domain.Enums;

namespace Domain.Specifications;

public sealed class PublicJobsSpec : Specification<Job>
{
    public PublicJobsSpec(int take)
    {
        Query
            .AsNoTracking()
            .Include(x => x.Employer)
            .Where(x => !x.Deleted && x.Status != JobStatus.Draft && x.Status != JobStatus.Deactivated)
            .OrderByDescending(x => x.CreatedOn)
            .Take(take);
    }
}
