using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Infrastructure.Services;
using Infrastructure.Services.AiScoring;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modules.TalentProfiles;
using Shared.Dtos.TalentProfiles;
using Shared.Exceptions;

namespace Modules.TalentProfiles.UploadResume;

public sealed class UploadTalentProfileResumeCommandHandler(
    AppDbContext dbContext,
    IResumeStorageService resumeStorageService,
    IAiScoreInvalidationService aiScoreInvalidationService,
    ILogger<UploadTalentProfileResumeCommandHandler> logger)
    : IRequestHandler<UploadTalentProfileResumeCommand, TalentProfileDto>
{
    private readonly AppDbContext _dbContext = dbContext;
    private readonly IResumeStorageService _resumeStorageService = resumeStorageService;
    private readonly IAiScoreInvalidationService _aiScoreInvalidationService = aiScoreInvalidationService;
    private readonly ILogger<UploadTalentProfileResumeCommandHandler> _logger = logger;

    public async Task<TalentProfileDto> Handle(UploadTalentProfileResumeCommand request, CancellationToken cancellationToken)
    {
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

        var profile = await _dbContext.TalentProfiles
            .Include(x => x.Skills)
            .FirstOrDefaultAsync(x => x.UserId == request.UserId, cancellationToken);

        var isNewProfile = profile is null;
        profile ??= new TalentProfile
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await using var resumeStream = request.Resume.OpenReadStream();
        var resumeUploadResult = await _resumeStorageService.UploadAsync(
            resumeStream,
            request.Resume.FileName,
            request.Resume.ContentType,
            cancellationToken);

        profile.ResumeUrl = resumeUploadResult.ObjectUrl;
        profile.ResumeFileName = Path.GetFileName(request.Resume.FileName);
        profile.UpdatedAt = DateTimeOffset.UtcNow;

        if (isNewProfile)
        {
            _dbContext.TalentProfiles.Add(profile);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            await _aiScoreInvalidationService.EnqueueScoresForTalentAsync(
                profile.Id,
                "resume-updated",
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to queue AI score refresh after resume upload. TalentId: {TalentId}",
                profile.Id);
        }

        return TalentProfileMappings.MapToDto(user, profile);
    }
}
