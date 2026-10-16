using Ardalis.Specification;
using Domain.Entities;

namespace Domain.Specifications;

public sealed class EmployerByUserIdSpec : Specification<Employer>
{
    public EmployerByUserIdSpec(Guid userId)
    {
        Query.Where(x => x.UserId == userId);
    }
}
