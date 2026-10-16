using Ardalis.Specification;
using Domain.Entities;

namespace Domain.Specifications;

public sealed class AiScoresByJobIdSpec : Specification<AiScore>
{
    public AiScoresByJobIdSpec(Guid jobId)
    {
        Query.Where(s => s.JobId == jobId);
    }
}
