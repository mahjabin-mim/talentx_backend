using Domain.Entities;
using Domain.Repositories;
using MediatR;
using Shared.Dtos.TalentProfiles;
using Domain.Specifications;
using Shared.Exceptions;

namespace Modules.Jobs.GetTalentProfileDetail;

public sealed class GetTalentProfileDetailQueryHandler(
    IRepository<TalentProfile> talentProfileRepository)
    : IRequestHandler<GetTalentProfileDetailQuery, TalentProfileDto>
{
    private readonly IRepository<TalentProfile> _talentProfileRepository = talentProfileRepository;

    public async Task<TalentProfileDto> Handle(GetTalentProfileDetailQuery request, CancellationToken cancellationToken)
    {
        var profile = await _talentProfileRepository.FirstOrDefaultAsync(
            new TalentProfileByIdWithDetailsSpec(request.TalentId),
            cancellationToken);

        if (profile is null)
        {
            throw new ResourceNotFoundException("Talent profile not found.");
        }

        return new TalentProfileDto
        {
            Name = profile.User?.Fullname ?? string.Empty,
            Email = profile.User?.Email ?? string.Empty,
            ResumeUrl = profile.ResumeUrl ?? string.Empty,
            ResumeFileName = profile.ResumeFileName ?? string.Empty,
            PictureUrl = profile.PictureUrl ?? string.Empty,
            PictureFileName = profile.PictureFileName ?? string.Empty,
            Location = profile.Location ?? string.Empty,
            Tagline = profile.Tagline ?? string.Empty,
            Bio = profile.Bio ?? string.Empty,
            Linkedin = profile.Linkedin ?? string.Empty,
            AdditionalUrl = profile.AdditionalUrl ?? string.Empty,
            VideoIntro = profile.VideoIntro ?? string.Empty,
            Experience = profile.Experience,
            Workplaces = profile.Workplaces ?? string.Empty,
            ExpectedSalary = profile.ExpectedSalary,
            Skills = profile.Skills.Select(s => new TalentProfileSkillDto
            {
                Language = s.Language.ToString(),
                Experience = s.YearsOfExperience
            }).ToList()
        };
    }
}
