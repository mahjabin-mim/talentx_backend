using Ardalis.Specification;
using Domain.Entities;
using Domain.Enums;

namespace Domain.Specifications;

public sealed class JobsToExpireSpec : Specification<Job>
{
    public JobsToExpireSpec(DateTimeOffset now, Guid? employerId = null)
    {
        Query.Where(x =>
            !x.Deleted &&
            x.ApplicationDeadline < now &&
            x.Status != JobStatus.Closed &&
            x.Status != JobStatus.Expired);

        if (employerId.HasValue)
        {
            Query.Where(x => x.EmployerId == employerId.Value);
        }
    }
}
