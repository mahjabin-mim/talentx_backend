using Domain.Enums;
using Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Dtos.Jobs;

namespace Modules.Jobs.GetPublicJobs;

public sealed class GetPublicJobsQueryHandler(
    IJobRepository jobRepository,
    IUnitOfWork unitOfWork,
    ILogger<GetPublicJobsQueryHandler> logger) : IRequestHandler<GetPublicJobsQuery, GetPublicJobsResponseDto>
{
    private readonly IJobRepository _jobRepository = jobRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger<GetPublicJobsQueryHandler> _logger = logger;

    public async Task<GetPublicJobsResponseDto> Handle(GetPublicJobsQuery request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var jobsToExpire = await _jobRepository.GetPublicJobsToExpireAsync(now, cancellationToken);

        if (jobsToExpire.Count > 0)
        {
            foreach (var expiredJob in jobsToExpire)
            {
                expiredJob.Status = JobStatus.Expired;
                expiredJob.LastModifiedOn = now;
            }
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var (totalMatched, jobs) = await _jobRepository.GetPagedPublicJobsAsync(
            request.SearchTerm, request.EmploymentType, request.WorkArrangement, request.ExperienceLevel, request.Page, request.PageSize, cancellationToken);

        var totalPages = (int)Math.Ceiling(totalMatched / (double)request.PageSize);

        return new GetPublicJobsResponseDto
        {
            CurrentPage = request.Page,
            TotalPages = totalPages == 0 ? 1 : totalPages,
            TotalCount = totalMatched,
            Jobs = jobs.Select(x => new PublicJobCardDto
            {
                // ... Keep mapping exactly as you have it ...
                JobId = x.Id,
                Title = x.Title,
                CompanyName = x.Employer.CompanyName,
                EmploymentType = x.EmploymentType.ToString(),
                WorkArrangement = x.WorkArrangement.ToString(),
                ExperienceLevel = x.ExperienceLevel.ToString(),
                ExperienceMinYears = x.ExperienceMinYears,
                ExperienceMaxYears = x.ExperienceMaxYears,
                SalaryMin = x.SalaryMin,
                SalaryMax = x.SalaryMax,
                CurrencyCode = x.CurrencyCode,
                NumberOfPositions = x.NumberOfPositions,
                ApplicationDeadline = x.ApplicationDeadline,
                CreatedOn = x.CreatedOn,
                Status = x.Status.ToString(),
                Location = x.WorkArrangement == WorkArrangement.Remote ? x.Timezone : (string.IsNullOrWhiteSpace(x.City) ? x.CountryCode : $"{x.City}, {x.CountryCode}")
            }).ToList(),
        };
    }
}