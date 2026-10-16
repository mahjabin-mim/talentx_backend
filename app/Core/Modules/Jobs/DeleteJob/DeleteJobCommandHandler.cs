using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Domain.Specifications;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Exceptions;

namespace Modules.Jobs.DeleteJob;

public sealed class DeleteJobCommandHandler(
    IRepository<User> userRepository,
    IRepository<Domain.Entities.Employer> employerRepository,
    IRepository<Job> jobRepository,
    IUnitOfWork unitOfWork,
    ILogger<DeleteJobCommandHandler> logger) : IRequestHandler<DeleteJobCommand, Unit>
{
    private readonly IRepository<User> _userRepository = userRepository;
    private readonly IRepository<Domain.Entities.Employer> _employerRepository = employerRepository;
    private readonly IRepository<Job> _jobRepository = jobRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger<DeleteJobCommandHandler> _logger = logger;

    public async Task<Unit> Handle(DeleteJobCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.FirstOrDefaultAsync(new UserByIdSpec(request.UserId), cancellationToken);
        if (user is null || !user.IsActive || user.UserRole != UserRole.Employer)
        {
            _logger.LogInformation("DeleteJob denied: Employer account unavailable. UserId: {UserId}", request.UserId);
            throw new UnauthorizedException("Employer account is unavailable.");
        }

        var employer = await _employerRepository.FirstOrDefaultAsync(new EmployerByUserIdSpec(request.UserId), cancellationToken)
            ?? throw new ValidationException("Employer profile not found.");

        var job = await _jobRepository.FirstOrDefaultAsync(new JobByIdAndEmployerSpec(request.JobId, employer.Id), cancellationToken)
            ?? throw new ValidationException("Job not found.");

        job.Deleted = true;
        job.DeletedOn = DateTimeOffset.UtcNow;
        job.DeletedBy = user.Email;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Job soft deleted. JobId: {JobId}, EmployerId: {EmployerId}", job.Id, employer.Id);

        return Unit.Value;
    }
}
