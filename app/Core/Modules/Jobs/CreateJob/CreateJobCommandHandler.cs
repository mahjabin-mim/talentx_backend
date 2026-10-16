using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Domain.Specifications;
using FluentValidation;
using Infrastructure.Services.AiScoring;
using MediatR;
using Microsoft.Extensions.Logging;
using Modules.Jobs;
using Shared.Dtos.Jobs;
using Shared.Exceptions;

namespace Modules.Jobs.CreateJob;

public sealed class CreateJobCommandHandler(
    IRepository<User> userRepository,
    IRepository<Domain.Entities.Employer> employerRepository,
    IRepository<Job> jobRepository,
    IUnitOfWork unitOfWork,
    IAiScoreInvalidationService aiScoreInvalidationService,
    ILogger<CreateJobCommandHandler> logger) : IRequestHandler<CreateJobCommand, CreateJobResponseDto>
{
    private readonly IRepository<User> _userRepository = userRepository;
    private readonly IRepository<Domain.Entities.Employer> _employerRepository = employerRepository;
    private readonly IRepository<Job> _jobRepository = jobRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IAiScoreInvalidationService _aiScoreInvalidationService = aiScoreInvalidationService;
    private readonly ILogger<CreateJobCommandHandler> _logger = logger;

    public async Task<CreateJobResponseDto> Handle(CreateJobCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.FirstOrDefaultAsync(new ActiveEmployerUserByIdSpec(request.UserId), cancellationToken);

        if (user is null)
        {
            throw new UnauthorizedException("Employer account is unavailable.");
        }

        var employer = await _employerRepository.FirstOrDefaultAsync(new EmployerByUserIdSpec(request.UserId), cancellationToken);

        if (employer is null)
        {
            var fallbackCompanyName = string.IsNullOrWhiteSpace(user.Fullname)
                ? "Employer Company"
                : user.Fullname.Trim();

            employer = new Domain.Entities.Employer
            {
                UserId = user.Id,
                CompanyName = fallbackCompanyName.Length > 160
                    ? fallbackCompanyName[..160]
                    : fallbackCompanyName
            };

            await _employerRepository.AddAsync(employer, cancellationToken);
        }

        if (!Enum.TryParse<EmploymentType>(request.EmploymentType, true, out var employmentType))
        {
            throw new BadRequestException("Unsupported employment type.");
        }

        if (!Enum.TryParse<WorkArrangement>(request.WorkArrangement, true, out var workArrangement))
        {
            throw new BadRequestException("Unsupported work arrangement.");
        }

        if (!Enum.TryParse<ExperienceLevel>(request.ExperienceLevel, true, out var experienceLevel))
        {
            throw new BadRequestException("Unsupported experience level.");
        }

        var countryCode = workArrangement == WorkArrangement.Remote
            ? string.Empty
            : request.CountryCode!.Trim().ToUpperInvariant();

        var city = workArrangement == WorkArrangement.Remote
            ? string.Empty
            : request.City!.Trim();

        var timezone = workArrangement == WorkArrangement.Remote
            ? request.Timezone!.Trim()
            : string.Empty;

        var now = DateTimeOffset.UtcNow;
        var job = new Job
        {
            CreatedOn = now,
            Deleted = false,
            Status = JobStatus.Draft
        };

        if (employer.Id == Guid.Empty)
        {
            job.Employer = employer;
        }
        else
        {
            job.EmployerId = employer.Id;
        }

        await _jobRepository.AddAsync(job, cancellationToken);

        var manualDescription = request.Description.Trim();

        job.Title = request.Title.Trim();
        job.Description = manualDescription;
        job.Responsibilities = request.Responsibilities.Trim();
        JobMutationHelpers.ReplaceSkills(job, request.Skills);
        job.EmploymentType = employmentType;
        job.WorkArrangement = workArrangement;
        job.ExperienceLevel = experienceLevel;
        job.ExperienceMinYears = request.ExperienceMinYears;
        job.ExperienceMaxYears = request.ExperienceMaxYears;
        job.SalaryMin = request.SalaryMin;
        job.SalaryMax = request.SalaryMax;
        job.CountryCode = countryCode;
        job.City = city;
        job.Timezone = timezone;
        job.CurrencyCode = request.CurrencyCode.Trim().ToUpperInvariant();
        job.NumberOfPositions = request.NumberOfPositions;
        job.ApplicationDeadline = request.ApplicationDeadline;
        job.Status = request.PublishJob ? JobStatus.Active : JobStatus.Draft;
        job.LastModifiedBy = null;
        job.LastModifiedOn = now;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            await _aiScoreInvalidationService.EnqueueScoresForJobAsync(
                job.Id,
                "job-created",
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to queue AI score refresh after job creation. JobId: {JobId}", job.Id);
        }

        _logger.LogInformation(
            "Job created. JobId: {JobId}, EmployerId: {EmployerId}, UserId: {UserId}",
            job.Id,
            employer.Id,
            request.UserId);

        return new CreateJobResponseDto
        {
            JobId = job.Id,
            Status = job.Status.ToString(),
            Description = job.Description,
            Message = request.PublishJob
                ? "Job published successfully."
                : "Job draft saved with manual description."
        };
    }
}
