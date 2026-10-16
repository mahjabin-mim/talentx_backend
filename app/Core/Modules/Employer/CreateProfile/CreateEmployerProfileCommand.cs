using MediatR;

namespace Modules.Employer.CreateProfile;

public sealed record CreateEmployerProfileCommand(Guid UserId, string FullName, string CompanyName) : IRequest;
