using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Domain.Specifications;
using Infrastructure.Services.AiScoring;
using MediatR;
using Microsoft.Extensions.Logging;
using Modules.Jobs;
using Shared.Dtos.Jobs;
using Shared.Exceptions;

namespace Modules.Jobs.UpdateJob;

public sealed class UpdateJobCommandHandler(
    IRepository<User> userRepository,
    IRepository<Domain.Entities.Employer> employerRepository,
    IRepository<Domain.Entities.Job> jobRepository,
    IUnitOfWork unitOfWork,
    IAiScoreInvalidationService aiScoreInvalidationService,
    ILogger<UpdateJobCommandHandler> logger) : IRequestHandler<UpdateJobCommand, CreateJobResponseDto>
{
    private readonly IRepository<User> _userRepository = userRepository;
    private readonly IRepository<Domain.Entities.Employer> _employerRepository = employerRepository;
    private readonly IRepository<Domain.Entities.Job> _jobRepository = jobRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IAiScoreInvalidationService _aiScoreInvalidationService = aiScoreInvalidationService;
    private readonly ILogger<UpdateJobCommandHandler> _logger = logger;

    public async Task<CreateJobResponseDto> Handle(UpdateJobCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.FirstOrDefaultAsync(new ActiveEmployerUserByIdSpec(request.UserId), cancellationToken);

        if (user is null)
        {
            throw new UnauthorizedException("Employer account is unavailable.");
        }

        var employer = await _employerRepository.FirstOrDefaultAsync(new EmployerByUserIdSpec(request.UserId), cancellationToken)
            ?? throw new ResourceNotFoundException("Employer profile not found.");

        var job = await _jobRepository.FirstOrDefaultAsync(new JobByIdAndEmployerSpec(request.JobId, employer.Id), cancellationToken)
            ?? throw new ResourceNotFoundException("Job not found.");

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

        job.Title = request.Title.Trim();
        job.Description = request.Description.Trim();
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
                "job-updated",
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to queue AI score refresh after job update. JobId: {JobId}", job.Id);
        }

        _logger.LogInformation(
            "Job updated. JobId: {JobId}, EmployerId: {EmployerId}, UserId: {UserId}",
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
                : "Job draft updated successfully."
        };
    }
}
