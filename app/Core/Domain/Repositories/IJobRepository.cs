using Domain.Entities;
using Domain.Enums;

namespace Domain.Repositories;

public interface IJobRepository
{
    Task<Job?> GetByIdAndEmployerAsync(Guid jobId, Guid employerId, CancellationToken cancellationToken = default);
    Task<Job?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken = default);
    void Add(Job job);
    void Remove(Job job);

    Task<List<Job>> GetJobsToExpireAsync(Guid employerId, DateTimeOffset now, CancellationToken cancellationToken = default);
    Task<Dictionary<JobStatus, int>> GetStatusCountsAsync(Guid employerId, CancellationToken cancellationToken = default);
    Task<(int TotalMatched, List<Job> Jobs)> GetPagedDashboardJobsAsync(Guid employerId, string? searchTerm, string? statusFilter, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<List<Job>> GetPublicJobsToExpireAsync(DateTimeOffset now, CancellationToken cancellationToken = default);
    Task<List<Job>> GetPublicJobsAsync(int take, CancellationToken cancellationToken = default);
    Task<(int TotalMatched, List<Job> Jobs)> GetPagedPublicJobsAsync(string? searchTerm, string? employmentType, string? workArrangement, string? experienceLevel, int page, int pageSize, CancellationToken cancellationToken = default);
}