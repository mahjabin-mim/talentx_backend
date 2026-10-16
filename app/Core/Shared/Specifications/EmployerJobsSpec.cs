using Ardalis.Specification;
using Domain.Entities;

namespace Domain.Specifications;

public sealed class EmployerJobsSpec : Specification<Job>
{
    public EmployerJobsSpec(Guid employerId)
    {
        Query
            .AsNoTracking()
            .Where(x => x.EmployerId == employerId && !x.Deleted);
    }
}
