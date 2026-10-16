using Ardalis.Specification;
using Domain.Entities;

namespace Domain.Specifications;

public sealed class TalentsNotAppliedForJobSpec : Specification<TalentProfile>
{
    public TalentsNotAppliedForJobSpec(
        Guid jobId,
        IEnumerable<Guid> excludedTalentIds,
        string? searchTerm = null,
        int? minExperience = null)
    {
        Query.Include(x => x.User);

        Query.Where(x => !excludedTalentIds.Contains(x.Id));

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var search = searchTerm.Trim().ToLower();
            Query.Where(x => (x.User.Fullname != null && x.User.Fullname.ToLower().Contains(search)) || 
                            (x.Tagline != null && x.Tagline.ToLower().Contains(search)));
        }

        if (minExperience.HasValue)
        {
            Query.Where(x => x.Experience >= minExperience.Value);
        }
        
        Query.OrderByDescending(x => x.CreatedAt);
    }
}
