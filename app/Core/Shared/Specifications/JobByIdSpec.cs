using Ardalis.Specification;
using Domain.Entities;

namespace Domain.Specifications;

public sealed class JobByIdSpec : Specification<Job>
{
    public JobByIdSpec(Guid jobId)
    {
        Query
            .Where(x => x.Id == jobId && !x.Deleted)
            .Include(x => x.Employer)
            .Include(x => x.Skills);
    }
}
