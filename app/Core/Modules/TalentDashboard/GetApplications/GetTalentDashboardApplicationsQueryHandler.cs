using Domain.Enums;
using Domain.Repositories;
using Domain.Specifications;
using Infrastructure.Data;
using Infrastructure.Options;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shared.Dtos.TalentDashboard;
using Shared.Exceptions;
using Domain.Entities;

namespace Modules.TalentDashboard.GetApplications;

public sealed class GetTalentDashboardApplicationsQueryHandler(
    IUserRepository userRepository,
    IRepository<Application> applicationRepository,
    AppDbContext dbContext,
    IOptions<AiScoringOptions> aiScoringOptions)
    : IRequestHandler<GetTalentDashboardApplicationsQuery, GetTalentDashboardApplicationsResponseDto>
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IRepository<Application> _applicationRepository =
        applicationRepository;
    private readonly AppDbContext _dbContext = dbContext;
    private readonly AiScoringOptions _aiScoringOptions = aiScoringOptions.Value;

    public async Task<GetTalentDashboardApplicationsResponseDto> Handle(
        GetTalentDashboardApplicationsQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(
            request.UserId,
            cancellationToken);

        if (user is null)
        {
            throw new ResourceNotFoundException("User not found.");
        }

        if (!user.IsActive || user.UserRole != UserRole.Talent)
        {
            throw new UnauthorizedException("Talent account is unavailable.");
        }

        var applications = await _applicationRepository.ListAsync(
            new TalentDashboardApplicationsByUserIdSpec(request.UserId),
            cancellationToken);
        var totalApplications = applications.Count;
        var totalApplicationPages = totalApplications == 0
            ? 0
            : (int)Math.Ceiling(totalApplications / (double)request.PageSize);
        var pagedApplications = applications
            .Skip((request.ApplicationsPage - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var applicationJobIds = pagedApplications
            .Select(x => x.JobId)
            .Distinct()
            .ToList();

        var applicationTalentIds = pagedApplications
            .Select(x => x.TalentId)
            .Distinct()
            .ToList();

        var aiScores = await _dbContext.AiScores
            .AsNoTracking()
            .Where(x => applicationJobIds.Contains(x.JobId) && applicationTalentIds.Contains(x.TalentId))
            .ToDictionaryAsync(
                x => (x.JobId, x.TalentId),
                x => x.Score,
                cancellationToken);

        var appliedJobIds = applications
            .Select(x => x.JobId)
            .Distinct()
            .ToList();

        var allInvitations = await GetInvitationsAsync(
            request.UserId,
            appliedJobIds,
            cancellationToken);
        var totalInvitations = allInvitations.Count;
        var totalInvitationPages = totalInvitations == 0
            ? 0
            : (int)Math.Ceiling(totalInvitations / (double)request.PageSize);
        var pagedInvitations = allInvitations
            .Skip((request.InvitationsPage - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return new GetTalentDashboardApplicationsResponseDto
        {
            Applications = pagedApplications
                .Select(application => MapToAppliedJob(
                    application,
                    aiScores.TryGetValue((application.JobId, application.TalentId), out var score)
                        ? score
                        : null))
                .ToList(),
            Invitations = pagedInvitations,
            ApplicationsPagination = new TalentDashboardPaginationDto
            {
                Page = request.ApplicationsPage,
                PageSize = request.PageSize,
                TotalItems = totalApplications,
                TotalPages = totalApplicationPages
            },
            InvitationsPagination = new TalentDashboardPaginationDto
            {
                Page = request.InvitationsPage,
                PageSize = request.PageSize,
                TotalItems = totalInvitations,
                TotalPages = totalInvitationPages
            },
            Summary = new TalentDashboardSummaryDto
            {
                TotalApplied = totalApplications,
                UnderReviewCount = applications.Count(x =>
                    x.Status == ApplicationStatus.Shortlisted),
                InterviewCount = applications.Count(x =>
                    x.Status == ApplicationStatus.Interviewed),
                TotalInvitations = totalInvitations,
                RespondedCount = allInvitations.Count(x => x.Status == "Responded"),
                PendingCount = allInvitations.Count(x => x.Status == "Pending")
            },
        };
    }

    private async Task<List<TalentDashboardInvitationDto>> GetInvitationsAsync(
        Guid userId,
        List<Guid> appliedJobIds,
        CancellationToken cancellationToken)
    {
        var talentProfile = await _dbContext.TalentProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (talentProfile is null)
        {
            return [];
        }

        var storedInvitations = await _dbContext.Invitations
            .AsNoTracking()
            .Include(x => x.Job)
                .ThenInclude(x => x.Employer)
            .Where(x =>
                x.TalentId == talentProfile.Id &&
                !appliedJobIds.Contains(x.JobId) &&
                !x.Job.Deleted &&
                x.Job.Status == JobStatus.Active)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        var storedInvitationJobIds = storedInvitations
            .Select(x => x.JobId)
            .Distinct()
            .ToList();

        var derivedInvitations = await _dbContext.AiScores
            .AsNoTracking()
            .Include(x => x.Job)
                .ThenInclude(x => x.Employer)
            .Where(x =>
                x.TalentId == talentProfile.Id &&
                x.Score >= _aiScoringOptions.InvitationMatchThreshold &&
                !appliedJobIds.Contains(x.JobId) &&
                !storedInvitationJobIds.Contains(x.JobId) &&
                !x.Job.Deleted &&
                x.Job.Status == JobStatus.Active)
            .OrderByDescending(x => x.CalculatedAt)
            .ToListAsync(cancellationToken);

        var invitationDtos = storedInvitations
            .Select(MapToInvitation)
            .Concat(derivedInvitations.Select(MapToDerivedInvitation))
            .OrderByDescending(x => x.InvitedAt)
            .ToList();

        return invitationDtos;
    }

    private static TalentDashboardAppliedJobDto MapToAppliedJob(
        Domain.Entities.Application application,
        decimal? aiScoring)
    {
        var job = application.Job;
        var organization = string.IsNullOrWhiteSpace(job.Employer.CompanyName)
            ? "Unknown organization"
            : job.Employer.CompanyName.Trim();

        return new TalentDashboardAppliedJobDto
        {
            Id = application.Id,
            JobTitle = job.Title,
            Organization = organization,
            Status = MapStatus(application.Status),
            JobLink = $"/jobs?jobId={job.Id}",
            AppliedDate = application.CreatedAt,
            SalaryRange = BuildSalaryRange(job.CurrencyCode, job.SalaryMin, job.SalaryMax),
            Location = BuildLocation(job.WorkArrangement, job.City, job.CountryCode, job.Timezone),
            AiScoring = aiScoring
        };
    }

    private static TalentDashboardInvitationDto MapToInvitation(Invitation invitation)
    {
        var job = invitation.Job;
        var organization = string.IsNullOrWhiteSpace(job.Employer.CompanyName)
            ? "Unknown organization"
            : job.Employer.CompanyName.Trim();

        return new TalentDashboardInvitationDto
        {
            Id = invitation.Id,
            JobTitle = job.Title,
            Organization = organization,
            InvitedAt = invitation.CreatedAt,
            Status = MapInvitationStatus(invitation.Status),
            JobLink = $"/jobs?jobId={job.Id}",
            EmploymentType = MapEmploymentType(job.EmploymentType)
        };
    }

    private static TalentDashboardInvitationDto MapToDerivedInvitation(AiScore aiScore)
    {
        var job = aiScore.Job;
        var organization = string.IsNullOrWhiteSpace(job.Employer.CompanyName)
            ? "Unknown organization"
            : job.Employer.CompanyName.Trim();

        return new TalentDashboardInvitationDto
        {
            Id = aiScore.Id,
            JobTitle = job.Title,
            Organization = organization,
            InvitedAt = aiScore.CalculatedAt,
            Status = "Pending",
            JobLink = $"/jobs?jobId={job.Id}",
            EmploymentType = MapEmploymentType(job.EmploymentType)
        };
    }

    private static string MapStatus(ApplicationStatus status)
    {
        return status switch
        {
            ApplicationStatus.Applied => "Applied",
            ApplicationStatus.Shortlisted => "Shortlisted",
            ApplicationStatus.Interviewed => "Interview",
            ApplicationStatus.Hired => "Hired",
            ApplicationStatus.NotSelected => "Not Selected",
            _ => "Applied"
        };
    }

    private static string MapInvitationStatus(InvitationStatus status)
    {
        return status == InvitationStatus.Pending
            ? "Pending"
            : "Responded";
    }

    private static string MapEmploymentType(EmploymentType employmentType)
    {
        return employmentType switch
        {
            EmploymentType.FullTime => "Full Time",
            EmploymentType.PartTime => "Part Time",
            EmploymentType.Contract => "Contract",
            EmploymentType.Internship => "Internship",
            _ => employmentType.ToString()
        };
    }

    private static string BuildSalaryRange(string currencyCode, decimal min, decimal max)
    {
        var code = string.IsNullOrWhiteSpace(currencyCode) ? "BDT" : currencyCode.Trim();
        return $"{code} {min:0} - {max:0}";
    }

    private static string BuildLocation(
        WorkArrangement workArrangement,
        string? city,
        string? countryCode,
        string? timezone)
    {
        if (workArrangement == WorkArrangement.Remote)
        {
            return string.IsNullOrWhiteSpace(timezone) ? "Remote" : $"Remote ({timezone})";
        }

        var normalizedCity = string.IsNullOrWhiteSpace(city) ? null : city.Trim();
        var normalizedCountry = string.IsNullOrWhiteSpace(countryCode) ? null : countryCode.Trim();

        if (normalizedCity is not null && normalizedCountry is not null)
        {
            return $"{normalizedCity}, {normalizedCountry}";
        }

        return normalizedCity ?? normalizedCountry ?? "N/A";
    }
}
