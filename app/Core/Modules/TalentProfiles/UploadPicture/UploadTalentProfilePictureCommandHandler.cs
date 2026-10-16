using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Infrastructure.Options;
using Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Modules.TalentProfiles;
using Shared.Dtos.TalentProfiles;
using Shared.Exceptions;

namespace Modules.TalentProfiles.UploadPicture;

public sealed class UploadTalentProfilePictureCommandHandler(
    AppDbContext dbContext,
    IResumeStorageService resumeStorageService,
    IOptions<S3Options> s3Options)
    : IRequestHandler<UploadTalentProfilePictureCommand, TalentProfileDto>
{
    private readonly AppDbContext _dbContext = dbContext;
    private readonly IResumeStorageService _resumeStorageService = resumeStorageService;
    private readonly S3Options _s3Options = s3Options.Value;

    public async Task<TalentProfileDto> Handle(UploadTalentProfilePictureCommand request, CancellationToken cancellationToken)
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

        await using var pictureStream = request.Picture.OpenReadStream();
        var pictureUploadResult = await _resumeStorageService.UploadAsync(
            pictureStream,
            request.Picture.FileName,
            request.Picture.ContentType,
            cancellationToken,
            _s3Options.ProfilePictureFolder);

        profile.PictureUrl = pictureUploadResult.ObjectUrl;
        profile.PictureFileName = Path.GetFileName(request.Picture.FileName);
        profile.UpdatedAt = DateTimeOffset.UtcNow;

        if (isNewProfile)
        {
            _dbContext.TalentProfiles.Add(profile);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return TalentProfileMappings.MapToDto(user, profile);
    }
}
