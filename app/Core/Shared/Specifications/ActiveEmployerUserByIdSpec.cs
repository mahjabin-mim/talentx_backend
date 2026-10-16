using Ardalis.Specification;
using Domain.Entities;
using Domain.Enums;

namespace Domain.Specifications;

public sealed class ActiveEmployerUserByIdSpec : Specification<User>
{
    public ActiveEmployerUserByIdSpec(Guid userId)
    {
        Query.Where(x =>
            x.Id == userId &&
            x.IsActive &&
            x.UserRole == UserRole.Employer &&
            !x.Deleted);
    }
}
