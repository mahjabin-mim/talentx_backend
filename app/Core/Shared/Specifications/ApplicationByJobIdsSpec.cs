using Ardalis.Specification;
using Domain.Entities;
using System;
using System.Collections.Generic;

namespace Domain.Specifications;

public sealed class ApplicationByJobIdsSpec : Specification<Application>
{
    public ApplicationByJobIdsSpec(IEnumerable<Guid> jobIds)
    {
        Query.AsNoTracking().Where(x => jobIds.Contains(x.JobId));
    }
}
