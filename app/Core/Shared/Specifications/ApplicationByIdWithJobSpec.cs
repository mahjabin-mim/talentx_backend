using Ardalis.Specification;
using Domain.Entities;

namespace Domain.Specifications;

public sealed class ApplicationByIdWithJobSpec : Specification<Application>
{
    public ApplicationByIdWithJobSpec(Guid applicationId)
    {
        Query.Where(x => x.Id == applicationId)
             .Include(x => x.Job)
             .ThenInclude(x => x.Employer)
             .Include(x => x.Talent);
    }
}
