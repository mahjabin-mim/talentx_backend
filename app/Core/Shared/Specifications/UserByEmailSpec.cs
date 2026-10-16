using Ardalis.Specification;
using Domain.Entities;

namespace Domain.Specifications;

public sealed class UserByEmailSpec : Specification<User>
{
    public UserByEmailSpec(string email)
    {
        Query.Where(x => x.Email == email && !x.Deleted);
    }
}
