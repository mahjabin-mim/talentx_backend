using MediatR;

namespace Modules.Jobs.InviteTalent;

public record InviteTalentCommand(Guid UserId, Guid JobId, Guid TalentId) : IRequest;
