using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Domain.Specifications;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Exceptions;

namespace Modules.Jobs.UpdateApplicationStatus;

public sealed class UpdateApplicationStatusCommandHandler(
    IRepository<User> userRepository,
    IRepository<Domain.Entities.Employer> employerRepository,
    IApplicationRepository applicationRepository,
    IUnitOfWork unitOfWork,
    ILogger<UpdateApplicationStatusCommandHandler> logger) : IRequestHandler<UpdateApplicationStatusCommand, Unit>
{
    private readonly IRepository<User> _userRepository = userRepository;
    private readonly IRepository<Domain.Entities.Employer> _employerRepository = employerRepository;
    private readonly IApplicationRepository _applicationRepository = applicationRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger<UpdateApplicationStatusCommandHandler> _logger = logger;

    public async Task<Unit> Handle(UpdateApplicationStatusCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.FirstOrDefaultAsync(new UserByIdSpec(request.UserId), cancellationToken);
        if (user is null || !user.IsActive || user.UserRole != UserRole.Employer)
        {
            _logger.LogInformation("UpdateApplicationStatus denied: Employer account unavailable. UserId: {UserId}", request.UserId);
            throw new UnauthorizedException("Employer account is unavailable.");
        }

        var employer = await _employerRepository.FirstOrDefaultAsync(new EmployerByUserIdSpec(request.UserId), cancellationToken)
            ?? throw new ResourceNotFoundException("Employer profile not found.");

        var application = await _applicationRepository.FirstOrDefaultAsync(new ApplicationByIdWithJobSpec(request.ApplicationId), cancellationToken)
            ?? throw new ResourceNotFoundException("Application not found.");

        if (application.Job.EmployerId != employer.Id)
        {
            _logger.LogWarning("UpdateApplicationStatus denied: Employer {EmployerId} attempted to update application {ApplicationId} for job {JobId} not owned by them.", employer.Id, application.Id, application.JobId);
            throw new ForbiddenException("You are not authorized to update this application's status.");
        }

        if (Enum.TryParse<ApplicationStatus>(request.Status, true, out var newStatus))
        {
            application.Status = newStatus;
            application.UpdatedAt = DateTimeOffset.UtcNow;
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Application status updated successfully. ApplicationId: {ApplicationId}, NewStatus: {Status}, EmployerId: {EmployerId}",
                application.Id,
                application.Status,
                employer.Id);
        }
        else
        {
            throw new BadRequestException("Invalid application status.");
        }

        return Unit.Value;
    }
}
