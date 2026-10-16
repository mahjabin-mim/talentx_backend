using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Services;
using MediatR;
using Shared.Dtos.Jobs;
using Microsoft.EntityFrameworkCore;
using Domain.Specifications;
using Shared.Exceptions;

namespace Modules.Jobs.GetApplicantResume;

public sealed class GetApplicantResumeQueryHandler(
    IRepository<Application> applicationRepository,
    IResumeStorageService resumeStorageService)
    : IRequestHandler<GetApplicantResumeQuery, ResumeDownloadResult>
{
    private readonly IRepository<Application> _applicationRepository = applicationRepository;
    private readonly IResumeStorageService _resumeStorageService = resumeStorageService;

    public async Task<ResumeDownloadResult> Handle(GetApplicantResumeQuery request, CancellationToken cancellationToken)
    {
        var application = await _applicationRepository.FirstOrDefaultAsync(
            new ApplicationByIdWithJobSpec(request.ApplicationId), 
            cancellationToken);

        if (application is null)
        {
            throw new ResourceNotFoundException("Application not found.");
        }

        if (application.Job?.Employer is null)
        {
            throw new ResourceNotFoundException("Employer or Job information is missing.");
        }

        if (application.Job.Employer.UserId != request.EmployerUserId)
        {
            throw new UnauthorizedException("Access denied to this resume.");
        }

        var resumeUrl = !string.IsNullOrWhiteSpace(application.ResumeUrl) 
            ? application.ResumeUrl 
            : application.Talent?.ResumeUrl;

        if (string.IsNullOrWhiteSpace(resumeUrl))
        {
            throw new ResourceNotFoundException("No resume URL found for this applicant.");
        }

        return await _resumeStorageService.DownloadByUrlAsync(resumeUrl, cancellationToken);
    }
}
