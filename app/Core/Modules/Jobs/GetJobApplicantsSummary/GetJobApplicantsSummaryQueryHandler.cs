using Domain.Entities;
using Domain.Repositories;
using MediatR;
using Shared.Dtos.Jobs;
using Microsoft.EntityFrameworkCore;
using Domain.Specifications;

namespace Modules.Jobs.GetJobApplicantsSummary;

public sealed class GetJobApplicantsSummaryQueryHandler(
    IRepository<Job> jobRepository,
    IRepository<Application> applicationRepository,
    IRepository<AiScore> aiScoreRepository)
    : IRequestHandler<GetJobApplicantsSummaryQuery, GetJobApplicantsSummaryResponseDto>
{
    private readonly IRepository<Job> _jobRepository = jobRepository;
    private readonly IRepository<Application> _applicationRepository = applicationRepository;
    private readonly IRepository<AiScore> _aiScoreRepository = aiScoreRepository;

    public async Task<GetJobApplicantsSummaryResponseDto> Handle(GetJobApplicantsSummaryQuery request, CancellationToken cancellationToken)
    {
        var job = await _jobRepository.FirstOrDefaultAsync(new JobByIdForUserSpec(request.JobId), cancellationToken);
        if (job is null)
            throw new Exception("Job not found or access denied.");

        var allApplications = await _applicationRepository.ListAsync(new ApplicationsByJobIdSpec(request.JobId), cancellationToken);

        var aiScores = await _aiScoreRepository.ListAsync(new AiScoresByJobIdSpec(request.JobId), cancellationToken);
        var scoreDict = aiScores.ToDictionary(s => s.TalentId, s => (int?)Math.Round(s.Score));

        var filtered = string.IsNullOrWhiteSpace(request.Status)
            ? allApplications
            : allApplications.Where(a => a.Status.ToString().Equals(request.Status, StringComparison.OrdinalIgnoreCase)).ToList();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.Trim().ToLowerInvariant();
            filtered = filtered.Where(a =>
                (a.Talent?.User?.Fullname ?? a.Name ?? string.Empty).ToLowerInvariant().Contains(search)
            ).ToList();
        }

        var totalCount = filtered.Count;

        var sorted = filtered
            .OrderByDescending(a => scoreDict.TryGetValue(a.TalentId, out var score) ? score : -1)
            .ThenBy(a => a.Talent?.User?.Fullname ?? a.Name ?? string.Empty)
            .ToList();

        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);
        var currentPage = Math.Max(1, Math.Min(request.Page, totalPages == 0 ? 1 : totalPages));
        var paged = sorted.Skip((currentPage - 1) * request.PageSize).Take(request.PageSize).ToList();

        var applicants = paged.Select(app => new JobApplicantSummaryDto
        {
            ApplicationId = app.Id,
            TalentId = app.TalentId,
            Name = app.Talent?.User?.Fullname ?? app.Name ?? string.Empty,
            AiProfileMatchScore = scoreDict.TryGetValue(app.TalentId, out var score) ? score : null,
            Status = app.Status.ToString()
        }).ToList();

        return new GetJobApplicantsSummaryResponseDto
        {
            Applicants = applicants,
            CurrentPage = currentPage,
            TotalPages = totalPages == 0 ? 1 : totalPages,
            TotalCount = totalCount
        };
    }
}
