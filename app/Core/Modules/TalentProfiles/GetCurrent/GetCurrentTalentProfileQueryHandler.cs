using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Modules.TalentProfiles;
using Shared.Dtos.TalentProfiles;
using Shared.Exceptions;

namespace Modules.TalentProfiles.GetCurrent;

public sealed class GetCurrentTalentProfileQueryHandler(AppDbContext dbContext)
    : IRequestHandler<GetCurrentTalentProfileQuery, TalentProfileDto>
{
    private readonly AppDbContext _dbContext = dbContext;

    public async Task<TalentProfileDto> Handle(GetCurrentTalentProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
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
            .AsNoTracking()
            .Include(x => x.Skills)
            .FirstOrDefaultAsync(x => x.UserId == request.UserId, cancellationToken);

        return TalentProfileMappings.MapToDto(user, profile);
    }
}
