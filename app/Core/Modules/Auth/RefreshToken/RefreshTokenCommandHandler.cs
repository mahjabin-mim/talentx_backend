using Domain.Entities;
using Domain.Repositories;
using Domain.Specifications;
using Infrastructure.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Dtos.Auth;
using Shared.Exceptions;

namespace Modules.Auth.RefreshToken;

public sealed class RefreshTokenCommandHandler(
    IRepository<User> userRepository,
    IUnitOfWork unitOfWork,
    ISecretHashService secretHashService,
    ITokenService tokenService,
    ILogger<RefreshTokenCommandHandler> logger) : IRequestHandler<RefreshTokenCommand, AuthResponseDto>
{
    private readonly IRepository<User> _userRepository = userRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ISecretHashService _secretHashService = secretHashService;
    private readonly ITokenService _tokenService = tokenService;
    private readonly ILogger<RefreshTokenCommandHandler> _logger = logger;

    public async Task<AuthResponseDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.FirstOrDefaultAsync(new UserByIdSpec(request.UserId), cancellationToken);

        if (user is null || !user.IsActive)
        {
            _logger.LogInformation("Refresh denied: user not found/inactive. UserId: {UserId}", request.UserId);
            throw new UnauthorizedException("Invalid refresh token.");
        }

        var incomingHash = _secretHashService.ComputeSha256(request.RefreshToken);
        var now = DateTimeOffset.UtcNow;

        var isCurrentTokenValid = !string.IsNullOrWhiteSpace(user.RefreshToken)
            && string.Equals(user.RefreshToken, incomingHash, StringComparison.Ordinal)
            && user.RefreshTokenExpiryTime.HasValue
            && user.RefreshTokenExpiryTime.Value > now;

        var isPreviousTokenValidInGracePeriod = !string.IsNullOrWhiteSpace(user.PreviousRefreshToken)
            && string.Equals(user.PreviousRefreshToken, incomingHash, StringComparison.Ordinal)
            && user.PreviousRefreshTokenExpiryTime.HasValue
            && user.PreviousRefreshTokenExpiryTime.Value > now;

        if (!isCurrentTokenValid && !isPreviousTokenValidInGracePeriod)
        {
            _logger.LogInformation("Refresh denied: token mismatch or grace period expired. UserId: {UserId}", user.Id);
            throw new UnauthorizedException("Refresh token expired or invalid.");
        }

        var newRefreshToken = _tokenService.CreateRefreshToken();
        var newRefreshTokenHash = _secretHashService.ComputeSha256(newRefreshToken);

        if (isCurrentTokenValid)
        {
            user.PreviousRefreshToken = user.RefreshToken;
        }

        user.RefreshToken = newRefreshTokenHash;
        user.RefreshTokenExpiryTime = _tokenService.GetRefreshTokenExpiry();
        user.LastModifiedOn = now;
        user.LastModifiedBy = user.Id.ToString();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var accessToken = _tokenService.CreateAccessToken(user);

        _logger.LogInformation(
            "Refresh succeeded. UserId: {UserId}, Role: {Role}, WasGracePeriod: {Grace}",
            user.Id,
            user.UserRole,
            isPreviousTokenValidInGracePeriod);

        return new AuthResponseDto
        {
            Id = user.Id,
            Fullname = user.Fullname,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            UserRole = user.UserRole?.ToString(),
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            RefreshTokenExpiryTime = user.RefreshTokenExpiryTime ?? now,
            Message = "Token refreshed successfully."
        };
    }
}
