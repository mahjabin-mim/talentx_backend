using MediatR;
using Shared.Dtos.Jobs;

namespace Modules.Jobs.GetJobById;

public sealed record GetJobByIdQuery(Guid UserId, Guid JobId) : IRequest<GetJobDetailsResponseDto>;
