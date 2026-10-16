using Ardalis.Specification;
using Domain.Entities;

namespace Domain.Specifications;

public sealed class ApplicationsByJobIdSpec : Specification<Application>
{
    public ApplicationsByJobIdSpec(Guid jobId)
    {
        Query.Where(app => app.JobId == jobId)
             .Include(app => app.Talent)
             .ThenInclude(t => t.User);
    }
}
