using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Domain.Specifications;
using MediatR;
using Shared.Exceptions;

namespace Modules.Jobs.InviteTalent;

public sealed class InviteTalentCommandHandler(
    IRepository<Job> jobRepository,
    IRepository<Domain.Entities.Employer> employerRepository,
    IRepository<TalentProfile> talentProfileRepository,
    IRepository<Invitation> invitationRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<InviteTalentCommand>
{
    private readonly IRepository<Job> _jobRepository = jobRepository;
    private readonly IRepository<Domain.Entities.Employer> _employerRepository = employerRepository;
    private readonly IRepository<TalentProfile> _talentProfileRepository = talentProfileRepository;
    private readonly IRepository<Invitation> _invitationRepository = invitationRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task Handle(InviteTalentCommand request, CancellationToken cancellationToken)
    {
        var employer = await _employerRepository.FirstOrDefaultAsync(new EmployerByUserIdSpec(request.UserId), cancellationToken);
        if (employer is null)
        {
            throw new ResourceNotFoundException("Employer profile not found.");
        }

        var job = await _jobRepository.FirstOrDefaultAsync(new JobByIdAndEmployerSpec(request.JobId, employer.Id), cancellationToken);
        if (job is null)
        {
            throw new ResourceNotFoundException("Job not found or access denied.");
        }

        var talent = await _talentProfileRepository.FirstOrDefaultAsync(new TalentProfileByIdWithDetailsSpec(request.TalentId), cancellationToken);
        if (talent is null)
        {
            throw new ResourceNotFoundException("Talent profile not found.");
        }

        var existingInvitation = await _invitationRepository.FirstOrDefaultAsync(
            new InvitationByJobAndTalentSpec(request.JobId, request.TalentId), cancellationToken);

        if (existingInvitation is not null)
        {
            throw new ResourceConflictException("Invitation has already been sent to this talent for this job.");
        }

        var invitation = new Invitation
        {
            Id = Guid.NewGuid(),
            JobId = request.JobId,
            TalentId = request.TalentId,
            Status = InvitationStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _invitationRepository.AddAsync(invitation, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
