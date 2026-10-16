using Domain.Entities;
using Domain.Repositories;
using MediatR;
using Shared.Dtos.Jobs;
using Microsoft.EntityFrameworkCore;
using Domain.Specifications;
using Shared.Exceptions;

namespace Modules.Jobs.GetTalentsNotApplied;

public sealed class GetTalentsNotAppliedQueryHandler(
    IRepository<Job> jobRepository,
    IRepository<Application> applicationRepository,
    IRepository<TalentProfile> talentProfileRepository,
    IRepository<AiScore> aiScoreRepository,
    IRepository<Invitation> invitationRepository)
    : IRequestHandler<GetTalentsNotAppliedQuery, GetTalentsNotAppliedResponseDto>
{
    private readonly IRepository<Job> _jobRepository = jobRepository;
    private readonly IRepository<Application> _applicationRepository = applicationRepository;
    private readonly IRepository<TalentProfile> _talentProfileRepository = talentProfileRepository;
    private readonly IRepository<AiScore> _aiScoreRepository = aiScoreRepository;
    private readonly IRepository<Invitation> _invitationRepository = invitationRepository;

    public async Task<GetTalentsNotAppliedResponseDto> Handle(GetTalentsNotAppliedQuery request, CancellationToken cancellationToken)
    {
        var job = await _jobRepository.FirstOrDefaultAsync(new JobByIdForUserSpec(request.JobId), cancellationToken);
        if (job is null)
        {
            throw new ResourceNotFoundException("Job not found or access denied.");
        }

        var appliedTalentIds = await _applicationRepository.ListAsync(new ApplicationsByJobIdSpec(request.JobId), cancellationToken);
        var excludedIds = appliedTalentIds.Select(a => a.TalentId).ToList();

        var aiScores = await _aiScoreRepository.ListAsync(new AiScoresByJobIdSpec(request.JobId), cancellationToken);
        var scoreDict = aiScores.ToDictionary(s => s.TalentId, s => (int?)Math.Round(s.Score));

        var invitations = await _invitationRepository.ListAsync(new InvitationsByJobIdSpec(request.JobId), cancellationToken);
        var invitedTalentIds = invitations.Select(i => i.TalentId).ToHashSet();

        var spec = new TalentsNotAppliedForJobSpec(request.JobId, excludedIds, request.SearchTerm, request.MinExperience);
        
        var allTalents = await _talentProfileRepository.ListAsync(spec, cancellationToken);
        
        var totalCount = allTalents.Count;

        var sortedTalents = allTalents
            .OrderByDescending(tp => scoreDict.TryGetValue(tp.Id, out var score) ? score : -1)
            .ThenBy(tp => tp.User.Fullname ?? string.Empty)
            .ToList();

        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);
        var currentPage = Math.Max(1, Math.Min(request.Page, totalPages == 0 ? 1 : totalPages));
        
        var pagedTalents = sortedTalents
            .Skip((currentPage - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var talents = pagedTalents.Select(tp => new TalentSummaryDto
        {
            TalentId = tp.Id,
            Name = tp.User.Fullname ?? string.Empty,
            Tagline = tp.Tagline ?? string.Empty,
            LinkedInUrl = tp.Linkedin ?? string.Empty,
            YearsOfExperience = tp.Experience,
            AiScore = scoreDict.TryGetValue(tp.Id, out var score) ? score : null,
            InvitationSent = invitedTalentIds.Contains(tp.Id)
        }).ToList();

        return new GetTalentsNotAppliedResponseDto
        {
            Talents = talents,
            CurrentPage = currentPage,
            TotalPages = totalPages == 0 ? 1 : totalPages,
            TotalCount = totalCount
        };
    }
}
