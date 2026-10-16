using MediatR;
using Shared.Dtos.TalentProfiles;

namespace Modules.Jobs.GetTalentProfileTextInfo;

public sealed record GetTalentProfileTextInfoQuery(Guid TalentId) : IRequest<TalentProfileTextInfoResponseDto>;
