using Domain.Entities;
using Domain.Repositories;
using Domain.Specifications;
using MediatR;

namespace Modules.Employer.ProfileExists;

public sealed class ProfileExistsQueryHandler : IRequestHandler<ProfileExistsQuery, ProfileExistsResponseDto>
{
    private readonly IRepository<Domain.Entities.Employer> _employerRepository;

    public ProfileExistsQueryHandler(IRepository<Domain.Entities.Employer> employerRepository)
    {
        _employerRepository = employerRepository;
    }

    public async Task<ProfileExistsResponseDto> Handle(ProfileExistsQuery request, CancellationToken cancellationToken)
    {
        var exists = await _employerRepository.AnyAsync(new EmployerByUserIdSpec(request.UserId), cancellationToken);
        return new ProfileExistsResponseDto { Exists = exists };
    }
}
