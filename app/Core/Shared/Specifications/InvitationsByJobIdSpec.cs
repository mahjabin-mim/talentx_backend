using Ardalis.Specification;
using Domain.Entities;

namespace Domain.Specifications;

public sealed class InvitationsByJobIdSpec : Specification<Invitation>
{
    public InvitationsByJobIdSpec(Guid jobId)
    {
        Query.Where(x => x.JobId == jobId);
    }
}
