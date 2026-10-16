using Ardalis.Specification;
using Domain.Entities;

namespace Domain.Specifications;

public sealed class InvitationByJobAndTalentSpec : Specification<Invitation>
{
    public InvitationByJobAndTalentSpec(Guid jobId, Guid talentId)
    {
        Query.Where(x => x.JobId == jobId && x.TalentId == talentId);
    }
}
