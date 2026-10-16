using Domain.Entities;
using Domain.Repositories;
using Domain.Specifications;
using Infrastructure.Services;
using MediatR;
using Shared.Exceptions;

namespace Modules.Auth.ChangePassword;

public sealed class ChangePasswordCommandHandler(
    IRepository<User> userRepository,
    IUnitOfWork unitOfWork,
    IPasswordService passwordService) : IRequestHandler<ChangePasswordCommand, Unit>
{
    private readonly IRepository<User> _userRepository = userRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IPasswordService _passwordService = passwordService;

    public async Task<Unit> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.FirstOrDefaultAsync(
            new UserByIdSpec(request.UserId),
            cancellationToken);

        if (user is null)
        {
            throw new UnauthorizedException("Invalid user context.");
        }

        if (!_passwordService.VerifyPassword(request.CurrentPassword, user.Password))
        {
            throw new UnauthorizedException("Current password is incorrect.");
        }

        user.Password = _passwordService.HashPassword(request.NewPassword);
        user.TokenVersion += 1;
        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;
        user.PreviousRefreshToken = null;
        user.PreviousRefreshTokenExpiryTime = null;
        user.LastModifiedOn = DateTimeOffset.UtcNow;
        user.LastModifiedBy = user.Id.ToString();

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
