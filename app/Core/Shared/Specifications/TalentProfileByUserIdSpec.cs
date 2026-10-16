using Ardalis.Specification;
using Domain.Entities;

namespace Domain.Specifications;

public sealed class TalentProfileByUserIdSpec : Specification<TalentProfile>
{
    public TalentProfileByUserIdSpec(Guid userId)
    {
        Query.Where(x => x.UserId == userId);
    }
}