using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Domain.Specifications;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Dtos.Jobs;
using Shared.Exceptions;

namespace Modules.Jobs.GetJobById;

public sealed class GetJobByIdQueryHandler(
    IRepository<User> userRepository,
    IRepository<Domain.Entities.Employer> employerRepository,
    IRepository<Job> jobRepository,
    IUnitOfWork unitOfWork,
    ILogger<GetJobByIdQueryHandler> logger) : IRequestHandler<GetJobByIdQuery, GetJobDetailsResponseDto>
{
    private readonly IRepository<User> _userRepository = userRepository;
    private readonly IRepository<Domain.Entities.Employer> _employerRepository = employerRepository;
    private readonly IRepository<Job> _jobRepository = jobRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger<GetJobByIdQueryHandler> _logger = logger;

    public async Task<GetJobDetailsResponseDto> Handle(GetJobByIdQuery request, CancellationToken cancellationToken)
    {
        if (request.UserId == Guid.Empty)
        {
            var job = await _jobRepository.FirstOrDefaultAsync(new JobByIdSpec(request.JobId), cancellationToken)
                ?? throw new ResourceNotFoundException("Job not found.");

            var now = DateTimeOffset.UtcNow;
            if (job.ApplicationDeadline < now && job.Status != JobStatus.Closed && job.Status != JobStatus.Expired)
            {
                job.Status = JobStatus.Expired;
                job.LastModifiedBy = null;
                job.LastModifiedOn = now;
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Job {JobId} automatically marked as expired (public access).", job.Id);
            }

            _logger.LogInformation("Job details retrieved successfully (public). JobId: {JobId}", job.Id);

            return new GetJobDetailsResponseDto
            {
                JobId = job.Id,
                CompanyName = job.Employer?.CompanyName ?? string.Empty,
                Title = job.Title,
                Description = job.Description,
                Responsibilities = job.Responsibilities,
                Skills = MapSkills(job),
                EmploymentType = job.EmploymentType.ToString(),
                WorkArrangement = job.WorkArrangement.ToString(),
                ExperienceLevel = job.ExperienceLevel.ToString(),
                ExperienceMinYears = job.ExperienceMinYears,
                ExperienceMaxYears = job.ExperienceMaxYears,
                SalaryMin = job.SalaryMin,
                SalaryMax = job.SalaryMax,
                CountryCode = string.IsNullOrWhiteSpace(job.CountryCode) ? null : job.CountryCode,
                City = string.IsNullOrWhiteSpace(job.City) ? null : job.City,
                Timezone = string.IsNullOrWhiteSpace(job.Timezone) ? null : job.Timezone,
                CurrencyCode = job.CurrencyCode,
                NumberOfPositions = job.NumberOfPositions,
                ApplicationDeadline = job.ApplicationDeadline,
                Status = job.Status.ToString(),
            };
        }
        else
        {
            var user = await _userRepository.FirstOrDefaultAsync(new ActiveEmployerUserByIdSpec(request.UserId), cancellationToken);
            if (user is null)
            {
                _logger.LogInformation("GetJobById denied: Employer account unavailable. UserId: {UserId}", request.UserId);
                throw new UnauthorizedException("Employer account is unavailable.");
            }

            var employer = await _employerRepository.FirstOrDefaultAsync(new EmployerByUserIdSpec(request.UserId), cancellationToken)
                ?? throw new ResourceNotFoundException("Employer profile not found.");

            var job = await _jobRepository.FirstOrDefaultAsync(new JobByIdAndEmployerSpec(request.JobId, employer.Id), cancellationToken)
                ?? throw new ResourceNotFoundException("Job not found.");

            var now = DateTimeOffset.UtcNow;
            if (job.ApplicationDeadline < now && job.Status != JobStatus.Closed && job.Status != JobStatus.Expired)
            {
                job.Status = JobStatus.Expired;
                job.LastModifiedBy = null;
                job.LastModifiedOn = now;
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Job {JobId} automatically marked as expired.", job.Id);
            }

            _logger.LogInformation("Job details retrieved successfully. JobId: {JobId}, EmployerId: {EmployerId}", job.Id, employer.Id);

            return new GetJobDetailsResponseDto
            {
                JobId = job.Id,
                CompanyName = job.Employer?.CompanyName ?? string.Empty,
                Title = job.Title,
                Description = job.Description,
                Responsibilities = job.Responsibilities,
                Skills = MapSkills(job),
                EmploymentType = job.EmploymentType.ToString(),
                WorkArrangement = job.WorkArrangement.ToString(),
                ExperienceLevel = job.ExperienceLevel.ToString(),
                ExperienceMinYears = job.ExperienceMinYears,
                ExperienceMaxYears = job.ExperienceMaxYears,
                SalaryMin = job.SalaryMin,
                SalaryMax = job.SalaryMax,
                CountryCode = string.IsNullOrWhiteSpace(job.CountryCode) ? null : job.CountryCode,
                City = string.IsNullOrWhiteSpace(job.City) ? null : job.City,
                Timezone = string.IsNullOrWhiteSpace(job.Timezone) ? null : job.Timezone,
                CurrencyCode = job.CurrencyCode,
                NumberOfPositions = job.NumberOfPositions,
                ApplicationDeadline = job.ApplicationDeadline,
                Status = job.Status.ToString(),
            };
        }
    }

    private static IReadOnlyList<string> MapSkills(Job job) =>
        job.Skills
            .OrderBy(x => x.DisplayOrder)
            .Select(x => x.Name)
            .ToList();
}
