using Ardalis.Specification;
using Domain.Entities;

namespace Domain.Specifications;

public sealed class JobByIdForUserSpec : Specification<Job>
{
    public JobByIdForUserSpec(Guid jobId)
    {
        Query.Where(job => job.Id == jobId);
    }
}
