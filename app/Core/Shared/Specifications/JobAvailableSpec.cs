using Ardalis.Specification;
using Domain.Entities;
using Domain.Enums;

namespace Domain.Specifications;

public sealed class JobAvailableSpec : Specification<Job>
{
    public JobAvailableSpec(Guid jobId, DateTimeOffset now)
    {
        Query.Where(x =>
            x.Id == jobId &&
            !x.Deleted &&
            x.Status == JobStatus.Active &&
            x.ApplicationDeadline > now);
    }
}