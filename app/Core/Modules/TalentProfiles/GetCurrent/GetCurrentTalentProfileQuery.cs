using MediatR;
using Shared.Dtos.TalentProfiles;

namespace Modules.TalentProfiles.GetCurrent;

public sealed record GetCurrentTalentProfileQuery(Guid UserId) : IRequest<TalentProfileDto>;
