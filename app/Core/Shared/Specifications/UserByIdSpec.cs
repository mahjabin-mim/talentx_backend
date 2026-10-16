using Ardalis.Specification;
using Domain.Entities;

namespace Domain.Specifications;

public sealed class UserByIdSpec : Specification<User>
{
    public UserByIdSpec(Guid userId)
    {
        Query.Where(x => x.Id == userId && !x.Deleted);
    }
}
