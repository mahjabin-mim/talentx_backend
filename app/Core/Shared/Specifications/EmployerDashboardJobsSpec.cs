using Ardalis.Specification;
using Domain.Entities;
using Domain.Enums;

namespace Domain.Specifications;

public sealed class EmployerDashboardJobsSpec : Specification<Job>
{
    public EmployerDashboardJobsSpec(
        Guid employerId,
        string? searchTerm,
        string? statusFilter,
        int? skip = null,
        int? take = null)
    {
        Query
            .AsNoTracking()
            .Where(x => x.EmployerId == employerId && !x.Deleted);

        if (!string.IsNullOrWhiteSpace(statusFilter)
            && !statusFilter.Equals("All", StringComparison.OrdinalIgnoreCase)
            && Enum.TryParse<JobStatus>(statusFilter, true, out var parsedStatus))
        {
            Query.Where(x => x.Status == parsedStatus);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var search = searchTerm.Trim().ToLowerInvariant();
            Query.Where(x =>
                x.Title.ToLower().Contains(search)
                || x.CountryCode.ToLower().Contains(search)
                || x.City.ToLower().Contains(search));
        }

        Query.OrderByDescending(x => x.CreatedOn);

        if (skip.HasValue)
        {
            Query.Skip(skip.Value);
        }

        if (take.HasValue)
        {
            Query.Take(take.Value);
        }
    }
}
