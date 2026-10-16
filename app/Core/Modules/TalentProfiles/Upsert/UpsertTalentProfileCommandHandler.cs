using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Infrastructure.Services.AiScoring;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modules.TalentProfiles;
using Shared.Dtos.TalentProfiles;
using Shared.Exceptions;

namespace Modules.TalentProfiles.Upsert;

public sealed class UpsertTalentProfileCommandHandler(
    AppDbContext dbContext,
    IAiScoreInvalidationService aiScoreInvalidationService,
    ILogger<UpsertTalentProfileCommandHandler> logger)
    : IRequestHandler<UpsertTalentProfileCommand, TalentProfileDto>
{
    private readonly AppDbContext _dbContext = dbContext;
    private readonly IAiScoreInvalidationService _aiScoreInvalidationService = aiScoreInvalidationService;
    private readonly ILogger<UpsertTalentProfileCommandHandler> _logger = logger;

    public async Task<TalentProfileDto> Handle(UpsertTalentProfileCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var normalizedPhoneNumber = Clean(request.PhoneNumber);
        var trimmedName = request.Name.Trim();

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == request.UserId && !x.Deleted, cancellationToken);

        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedException("Account is unavailable.");
        }

        if (user.UserRole != UserRole.Talent)
        {
            throw new UnauthorizedException("Talent profile is only available for talent accounts.");
        }

        var emailExists = await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(
                x => x.Id != request.UserId
                    && !x.Deleted
                    && x.Email == normalizedEmail,
                cancellationToken);

        if (emailExists)
        {
            throw new RegisterConflictException("Email is already registered.");
        }

        var profile = await _dbContext.TalentProfiles
            .FirstOrDefaultAsync(x => x.UserId == request.UserId, cancellationToken);

        var now = DateTimeOffset.UtcNow;

        if (profile is null)
        {
            profile = new TalentProfile
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                CreatedAt = now
            };

            _dbContext.TalentProfiles.Add(profile);
        }

        user.Fullname = trimmedName;
        user.Email = normalizedEmail;
        user.PhoneNumber = normalizedPhoneNumber;
        user.LastModifiedOn = now;
        user.LastModifiedBy = "talent-profile";

        profile.Location = Clean(request.Location);
        profile.Tagline = Clean(request.Tagline);
        profile.Bio = Clean(request.Bio);
        profile.Linkedin = Clean(request.Linkedin);
        profile.AdditionalUrl = Clean(request.AdditionalUrl);
        profile.VideoIntro = Clean(request.VideoIntro);
        profile.Experience = request.Experience;
        profile.Workplaces = Clean(request.Workplaces);
        profile.ExpectedSalary = request.ExpectedSalary;
        profile.UpdatedAt = now;

        await _dbContext.TalentProfileSkills
            .Where(x => x.TalentProfileId == profile.Id)
            .ExecuteDeleteAsync(cancellationToken);

        var updatedSkills = BuildSkills(profile.Id, request.Skills, now);

        if (updatedSkills.Count > 0)
        {
            _dbContext.TalentProfileSkills.AddRange(updatedSkills);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        try
        {
            await _aiScoreInvalidationService.EnqueueScoresForTalentAsync(
                profile.Id,
                "talent-profile-updated",
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to queue AI score refresh after talent profile update. TalentId: {TalentId}",
                profile.Id);
        }

        profile.Skills = updatedSkills;
        return TalentProfileMappings.MapToDto(user, profile);
    }

    private static string? Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static List<TalentProfileSkill> BuildSkills(
        Guid talentProfileId,
        IReadOnlyList<UpsertTalentProfileSkillRequestDto> requestedSkills,
        DateTimeOffset now)
    {
        var skills = new List<TalentProfileSkill>(requestedSkills.Count);

        foreach (var (skill, index) in requestedSkills.Select((value, index) => (value, index)))
        {
            if (!TalentProfileMappings.TryParseSkillLanguage(skill.Language, out var language))
            {
                continue;
            }

            skills.Add(new TalentProfileSkill
            {
                Id = Guid.NewGuid(),
                TalentProfileId = talentProfileId,
                Language = language,
                YearsOfExperience = skill.Experience,
                DisplayOrder = index,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        return skills;
    }
}
