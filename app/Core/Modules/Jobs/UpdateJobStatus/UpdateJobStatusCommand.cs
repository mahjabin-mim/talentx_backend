using MediatR;

namespace Modules.Jobs.UpdateJobStatus;

public sealed record UpdateJobStatusCommand(Guid UserId, Guid JobId, string Action) : IRequest<Unit>;
