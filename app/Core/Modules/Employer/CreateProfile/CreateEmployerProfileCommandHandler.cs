using Domain.Entities;
using Domain.Repositories;
using Domain.Specifications;
using MediatR;
using Shared.Exceptions;

namespace Modules.Employer.CreateProfile;

public sealed class CreateEmployerProfileCommandHandler : IRequestHandler<CreateEmployerProfileCommand>
{
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Domain.Entities.Employer> _employerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateEmployerProfileCommandHandler(
        IRepository<User> userRepository,
        IRepository<Domain.Entities.Employer> employerRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _employerRepository = employerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(CreateEmployerProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.FirstOrDefaultAsync(new UserByIdSpec(request.UserId), cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedException("User not found or inactive.");
        }

        var existingEmployer = await _employerRepository.FirstOrDefaultAsync(new EmployerByUserIdSpec(request.UserId), cancellationToken);
        if (existingEmployer is not null)
        {
            throw new ResourceConflictException("Employer profile already exists.");
        }

        user.Fullname = request.FullName.Trim();
        user.UserRole = Domain.Enums.UserRole.Employer;
        var employer = new Domain.Entities.Employer
        {
            UserId = user.Id,
            User = user,
            CompanyName = request.CompanyName.Trim()
        };
        await _employerRepository.AddAsync(employer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
