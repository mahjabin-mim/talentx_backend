using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class JobRepository : IJobRepository
{
    private readonly AppDbContext _dbContext;

    public JobRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Job?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Jobs
            .FirstOrDefaultAsync(x => x.Id == jobId && !x.Deleted, cancellationToken);
    }

    public async Task<Job?> GetByIdAndEmployerAsync(Guid jobId, Guid employerId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Jobs
            .FirstOrDefaultAsync(x => x.Id == jobId && x.EmployerId == employerId && !x.Deleted, cancellationToken);
    }

    public void Add(Job job) => _dbContext.Jobs.Add(job);
    public void Remove(Job job) => _dbContext.Jobs.Remove(job);

    public async Task<List<Job>> GetJobsToExpireAsync(Guid employerId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Jobs
            .Where(x => x.EmployerId == employerId && !x.Deleted && x.ApplicationDeadline < now && x.Status != JobStatus.Closed && x.Status != JobStatus.Expired)
            .ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<JobStatus, int>> GetStatusCountsAsync(Guid employerId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Jobs
            .AsNoTracking()
            .Where(x => x.EmployerId == employerId && !x.Deleted)
            .GroupBy(x => x.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(k => k.Status, v => v.Count, cancellationToken);
    }

    public async Task<(int TotalMatched, List<Job> Jobs)> GetPagedDashboardJobsAsync(Guid employerId, string? searchTerm, string? statusFilter, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var filterQuery = _dbContext.Jobs.AsNoTracking().Where(x => x.EmployerId == employerId && !x.Deleted);

        if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "All" && Enum.TryParse<JobStatus>(statusFilter, out var parsedStatus))
        {
            filterQuery = filterQuery.Where(x => x.Status == parsedStatus);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var search = searchTerm.ToLower();
            filterQuery = filterQuery.Where(x => x.Title.ToLower().Contains(search) || x.CountryCode.ToLower().Contains(search) || x.City.ToLower().Contains(search));
        }

        var totalMatched = await filterQuery.CountAsync(cancellationToken);
        var actualPage = Math.Max(1, Math.Min(page, (int)Math.Ceiling(totalMatched / (double)pageSize) == 0 ? 1 : (int)Math.Ceiling(totalMatched / (double)pageSize)));

        var jobs = await filterQuery
            .OrderByDescending(x => x.CreatedOn)
            .Skip((actualPage - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (totalMatched, jobs);
    }

    public async Task<List<Job>> GetPublicJobsToExpireAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Jobs
            .Where(x => !x.Deleted && x.ApplicationDeadline < now && x.Status != JobStatus.Closed && x.Status != JobStatus.Expired)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Job>> GetPublicJobsAsync(int take, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Jobs
            .AsNoTracking()
            .Include(x => x.Employer)
            .Where(x => !x.Deleted && x.Status == JobStatus.Active)
            .OrderByDescending(x => x.CreatedOn)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<(int TotalMatched, List<Job> Jobs)> GetPagedPublicJobsAsync(
    string? searchTerm, string? employmentType, string? workArrangement, string? experienceLevel,
    int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var filterQuery = _dbContext.Jobs
            .AsNoTracking()
            .Include(x => x.Employer)
            .Where(x => !x.Deleted && x.Status == JobStatus.Active);

        if (!string.IsNullOrWhiteSpace(employmentType) && Enum.TryParse<EmploymentType>(employmentType, true, out var parsedEmp))
            filterQuery = filterQuery.Where(x => x.EmploymentType == parsedEmp);

        if (!string.IsNullOrWhiteSpace(workArrangement) && Enum.TryParse<WorkArrangement>(workArrangement, true, out var parsedWork))
            filterQuery = filterQuery.Where(x => x.WorkArrangement == parsedWork);

        if (!string.IsNullOrWhiteSpace(experienceLevel) && Enum.TryParse<ExperienceLevel>(experienceLevel, true, out var parsedExp))
            filterQuery = filterQuery.Where(x => x.ExperienceLevel == parsedExp);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var search = searchTerm.ToLower();
            filterQuery = filterQuery.Where(x => x.Title.ToLower().Contains(search) || x.Employer.CompanyName.ToLower().Contains(search));
        }

        var totalMatched = await filterQuery.CountAsync(cancellationToken);
        var actualPage = Math.Max(1, Math.Min(page, (int)Math.Ceiling(totalMatched / (double)pageSize) == 0 ? 1 : (int)Math.Ceiling(totalMatched / (double)pageSize)));

        var jobs = await filterQuery
            .OrderByDescending(x => x.CreatedOn)
            .Skip((actualPage - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (totalMatched, jobs);
    }
}
