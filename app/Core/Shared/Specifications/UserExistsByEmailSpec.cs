using Ardalis.Specification;
using Domain.Entities;

namespace Domain.Specifications;

public sealed class UserExistsByEmailSpec : Specification<User>
{
    public UserExistsByEmailSpec(string email)
    {
        Query.Where(x => x.Email == email);
    }
}
