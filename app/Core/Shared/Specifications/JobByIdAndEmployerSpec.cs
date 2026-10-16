using Ardalis.Specification;
using Domain.Entities;

namespace Domain.Specifications;

public sealed class JobByIdAndEmployerSpec : Specification<Job>
{
    public JobByIdAndEmployerSpec(Guid jobId, Guid employerId)
    {
        Query
            .Where(x => x.Id == jobId && x.EmployerId == employerId && !x.Deleted)
            .Include(x => x.Employer)
            .Include(x => x.Skills);
    }
}
