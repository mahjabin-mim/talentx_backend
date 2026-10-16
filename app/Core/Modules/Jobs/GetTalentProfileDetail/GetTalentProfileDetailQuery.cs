using MediatR;
using Shared.Dtos.TalentProfiles;

namespace Modules.Jobs.GetTalentProfileDetail;

public sealed record GetTalentProfileDetailQuery(Guid TalentId) : IRequest<TalentProfileDto>;
