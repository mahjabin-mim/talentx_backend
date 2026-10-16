using Domain.Entities;
using Domain.Repositories;
using Domain.Specifications;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Modules.Auth.Logout;

public sealed class LogoutUserCommandHandler(
    IRepository<User> userRepository,
    IUnitOfWork unitOfWork,
    ILogger<LogoutUserCommandHandler> logger) : IRequestHandler<LogoutUserCommand>
{
    private readonly IRepository<User> _userRepository = userRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger<LogoutUserCommandHandler> _logger = logger;

    public async Task Handle(LogoutUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.FirstOrDefaultAsync(new UserByIdSpec(request.UserId), cancellationToken);
        if (user is null) return;

        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;
        user.TokenVersion += 1;
        user.LastModifiedOn = DateTimeOffset.UtcNow;
        user.LastModifiedBy = user.Id.ToString();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Logout succeeded and token version bumped. UserId: {UserId}, NewTokenVersion: {TokenVersion}",
            user.Id,
            user.TokenVersion);
    }
}
