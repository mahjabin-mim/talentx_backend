using Ardalis.Specification;
using Domain.Entities;

namespace Domain.Specifications;

public sealed class ApplicationByEmailSpec : Specification<Application>
{
    public ApplicationByEmailSpec(string email)
    {
        Query.Where(x => x.Email == email);
    }
}
