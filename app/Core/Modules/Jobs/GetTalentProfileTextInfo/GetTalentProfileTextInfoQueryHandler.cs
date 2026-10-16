using Domain.Entities;
using Domain.Repositories;
using MediatR;
using Shared.Dtos.TalentProfiles;
using Domain.Specifications;
using Shared.Exceptions;

namespace Modules.Jobs.GetTalentProfileTextInfo;

public sealed class GetTalentProfileTextInfoQueryHandler(
    IRepository<TalentProfile> talentProfileRepository)
    : IRequestHandler<GetTalentProfileTextInfoQuery, TalentProfileTextInfoResponseDto>
{
    private readonly IRepository<TalentProfile> _talentProfileRepository = talentProfileRepository;

    public async Task<TalentProfileTextInfoResponseDto> Handle(GetTalentProfileTextInfoQuery request, CancellationToken cancellationToken)
    {
        var profile = await _talentProfileRepository.FirstOrDefaultAsync(
            new TalentProfileByIdWithDetailsSpec(request.TalentId),
            cancellationToken);

        if (profile is null)
        {
            throw new ResourceNotFoundException("Talent profile not found.");
        }

        return new TalentProfileTextInfoResponseDto
        {
            Name = profile.User?.Fullname ?? string.Empty,
            Email = profile.User?.Email ?? string.Empty,
            Location = profile.Location ?? string.Empty,
            Tagline = profile.Tagline ?? string.Empty,
            Bio = profile.Bio ?? string.Empty,
            Linkedin = profile.Linkedin ?? string.Empty,
            AdditionalUrl = profile.AdditionalUrl ?? string.Empty,
            VideoIntro = profile.VideoIntro ?? string.Empty,
            Workplaces = profile.Workplaces ?? string.Empty,
            Skills = profile.Skills.Select(s => s.Language.ToString()).ToList()
        };
    }
}
