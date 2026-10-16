using MediatR;

namespace Modules.Jobs.UpdateApplicationStatus;

public sealed record UpdateApplicationStatusCommand(
    Guid UserId,
    Guid ApplicationId,
    string Status) : IRequest<Unit>;
