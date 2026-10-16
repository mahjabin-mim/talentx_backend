using MediatR;
using Infrastructure.Services;

namespace Modules.Jobs.GetApplicantResume;

public sealed record GetApplicantResumeQuery(
    Guid ApplicationId,
    Guid EmployerUserId
) : IRequest<ResumeDownloadResult>;
