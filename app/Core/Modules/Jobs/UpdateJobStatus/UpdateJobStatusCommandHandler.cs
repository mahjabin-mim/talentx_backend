using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Domain.Specifications;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Exceptions;

namespace Modules.Jobs.UpdateJobStatus;

public sealed class UpdateJobStatusCommandHandler(
    IRepository<User> userRepository,
    IRepository<Domain.Entities.Employer> employerRepository,
    IRepository<Job> jobRepository,
    IUnitOfWork unitOfWork,
    ILogger<UpdateJobStatusCommandHandler> logger) : IRequestHandler<UpdateJobStatusCommand, Unit>
{
    private readonly IRepository<User> _userRepository = userRepository;
    private readonly IRepository<Domain.Entities.Employer> _employerRepository = employerRepository;
    private readonly IRepository<Job> _jobRepository = jobRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger<UpdateJobStatusCommandHandler> _logger = logger;

    public async Task<Unit> Handle(UpdateJobStatusCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.FirstOrDefaultAsync(new UserByIdSpec(request.UserId), cancellationToken);
        if (user is null || !user.IsActive || user.UserRole != UserRole.Employer)
        {
            _logger.LogInformation("UpdateJobStatus denied: Employer account unavailable. UserId: {UserId}", request.UserId);
            throw new UnauthorizedException("Employer account is unavailable.");
        }

        var employer = await _employerRepository.FirstOrDefaultAsync(new EmployerByUserIdSpec(request.UserId), cancellationToken)
            ?? throw new ValidationException("Employer profile not found.");

        var job = await _jobRepository.FirstOrDefaultAsync(new JobByIdAndEmployerSpec(request.JobId, employer.Id), cancellationToken)
            ?? throw new ValidationException("Job not found.");

        var now = DateTimeOffset.UtcNow;
        if (job.ApplicationDeadline < now && job.Status != JobStatus.Closed && job.Status != JobStatus.Expired)
        {
            job.Status = JobStatus.Expired;
            job.LastModifiedBy = request.UserId.ToString();
            job.LastModifiedOn = now;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var action = request.Action.Trim();
        if (action.Equals("Deactivate", StringComparison.OrdinalIgnoreCase))
        {
            if (job.Status != JobStatus.Active) throw new ValidationException("Only active jobs can be deactivated.");
            job.Status = JobStatus.Deactivated;
        }
        else
        {
            if (job.Status == JobStatus.Active) return Unit.Value;
            if (job.Status != JobStatus.Deactivated && job.Status != JobStatus.Draft) throw new ValidationException("Only draft or deactivated jobs can be activated.");
            job.Status = JobStatus.Active;
        }

        job.LastModifiedBy = request.UserId.ToString();
        job.LastModifiedOn = now;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Job status updated successfully. JobId: {JobId}, NewStatus: {Status}, EmployerId: {EmployerId}",
            job.Id,
            job.Status,
            employer.Id);

        return Unit.Value;
    }
}
