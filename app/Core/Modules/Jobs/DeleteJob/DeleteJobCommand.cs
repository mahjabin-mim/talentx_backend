using MediatR;

namespace Modules.Jobs.DeleteJob;

public sealed record DeleteJobCommand(Guid UserId, Guid JobId) : IRequest<Unit>;
