using MediatR;

namespace Modules.Employer.ProfileExists;

public sealed record ProfileExistsQuery(Guid UserId) : IRequest<ProfileExistsResponseDto>;
