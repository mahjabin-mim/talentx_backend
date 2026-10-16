using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Domain.Specifications;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Dtos.Jobs;
using Shared.Exceptions;

namespace Modules.Jobs.GetEmployerDashboard;

public sealed class GetEmployerDashboardQueryHandler(
    IRepository<User> userRepository,
    IRepository<Domain.Entities.Employer> employerRepository,
    IRepository<Job> jobRepository,
    IRepository<Application> applicationRepository,
    IUnitOfWork unitOfWork,
    ILogger<GetEmployerDashboardQueryHandler> logger) : IRequestHandler<GetEmployerDashboardQuery, GetEmployerDashboardResponseDto>
{
    private readonly IRepository<User> _userRepository = userRepository;
    private readonly IRepository<Domain.Entities.Employer> _employerRepository = employerRepository;
    private readonly IRepository<Job> _jobRepository = jobRepository;
    private readonly IRepository<Application> _applicationRepository = applicationRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger<GetEmployerDashboardQueryHandler> _logger = logger;

    public async Task<GetEmployerDashboardResponseDto> Handle(GetEmployerDashboardQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.FirstOrDefaultAsync(new UserByIdSpec(request.UserId), cancellationToken);
        if (user is null)
        {
            _logger.LogInformation("GetEmployerDashboard denied: User not found. UserId: {UserId}", request.UserId);
            throw new ResourceNotFoundException("User not found.");
        }

        if (!user.IsActive || user.UserRole != UserRole.Employer)
        {
            _logger.LogInformation("GetEmployerDashboard denied: Employer account unavailable. UserId: {UserId}", request.UserId);
            throw new UnauthorizedException("Employer account is unavailable.");
        }

        var employer = await _employerRepository.FirstOrDefaultAsync(new EmployerByUserIdSpec(request.UserId), cancellationToken);
        if (employer is null)
        {
            _logger.LogInformation("GetEmployerDashboard denied: Employer not found. UserId: {UserId}", request.UserId);
            throw new ResourceNotFoundException("Employer not found.");
        }

        var now = DateTimeOffset.UtcNow;
        var jobsToExpire = await _jobRepository.ListAsync(new JobsToExpireSpec(now, employer.Id), cancellationToken);

        if (jobsToExpire.Count > 0)
        {
            foreach (var expiredJob in jobsToExpire)
            {
                expiredJob.Status = JobStatus.Expired;
                expiredJob.LastModifiedBy = request.UserId.ToString();
                expiredJob.LastModifiedOn = now;
            }
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Automatically expired {Count} jobs for EmployerId: {EmployerId}", jobsToExpire.Count, employer.Id);
        }

        var allEmployerJobs = await _jobRepository.ListAsync(new EmployerJobsSpec(employer.Id), cancellationToken);
        var statusCounts = allEmployerJobs
            .GroupBy(x => x.Status)
            .ToDictionary(x => x.Key, x => x.Count());

        var matchedJobs = await _jobRepository.ListAsync(
            new EmployerDashboardJobsSpec(employer.Id, request.SearchTerm, request.StatusFilter),
            cancellationToken);
        var totalMatched = matchedJobs.Count;

        var totalPages = (int)Math.Ceiling(totalMatched / (double)request.PageSize);
        var actualPage = Math.Max(1, Math.Min(request.Page, totalPages == 0 ? 1 : totalPages));
        var pagedJobs = await _jobRepository.ListAsync(
            new EmployerDashboardJobsSpec(
                employer.Id,
                request.SearchTerm,
                request.StatusFilter,
                (actualPage - 1) * request.PageSize,
                request.PageSize),
            cancellationToken);

        // Fetch application counts for paged jobs
        var pagedJobIds = pagedJobs.Select(j => j.Id).ToList();
        var applications = await _applicationRepository.ListAsync(
            new ApplicationByJobIdsSpec(pagedJobIds),
            cancellationToken);
        var applicationCounts = applications
            .GroupBy(a => a.JobId)
            .ToDictionary(g => g.Key, g => g.Count());

        _logger.LogInformation("Dashboard fetched. EmployerId: {EmployerId}, TotalMatched: {Total}", employer.Id, totalMatched);

        return new GetEmployerDashboardResponseDto
        {
            JobsPosted = statusCounts.Values.Sum() - statusCounts.GetValueOrDefault(JobStatus.Draft, 0),
            ActiveJobs = statusCounts.GetValueOrDefault(JobStatus.Active, 0),
            ClosedJobs = statusCounts.GetValueOrDefault(JobStatus.Closed, 0),
            AvailableStatuses = Enum.GetNames<JobStatus>(),
            Jobs = pagedJobs.Select(x => new EmployerDashboardJobDto
            {
                JobId = x.Id,
                Title = x.Title,
                Status = x.Status.ToString(),
                CreatedOn = x.CreatedOn,
                Applicants = applicationCounts.TryGetValue(x.Id, out var count) ? count : 0,
                Location = x.WorkArrangement == WorkArrangement.Remote ? x.Timezone : (string.IsNullOrWhiteSpace(x.City) ? x.CountryCode : $"{x.City}, {x.CountryCode}")
            }).ToList(),
            CurrentPage = actualPage,
            TotalPages = totalPages == 0 ? 1 : totalPages
        };
    }
}
