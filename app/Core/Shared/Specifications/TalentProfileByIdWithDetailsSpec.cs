using Ardalis.Specification;
using Domain.Entities;

namespace Domain.Specifications;

public sealed class TalentProfileByIdWithDetailsSpec : Specification<TalentProfile>
{
    public TalentProfileByIdWithDetailsSpec(Guid id)
    {
        Query.Where(x => x.Id == id)
             .Include(x => x.User)
             .Include(x => x.Skills);
    }
}
