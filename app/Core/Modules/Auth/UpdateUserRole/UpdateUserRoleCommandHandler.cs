using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Domain.Specifications;
using Infrastructure.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Dtos.Auth;
using Shared.Exceptions;

namespace Modules.Auth.UpdateUserRole;

public sealed class UpdateUserRoleCommandHandler(
    IRepository<User> userRepository,
    IRepository<Domain.Entities.Employer> employerRepository,
    IUnitOfWork unitOfWork,
    ISecretHashService secretHashService,
    ITokenService tokenService,
    ILogger<UpdateUserRoleCommandHandler> logger) : IRequestHandler<UpdateUserRoleCommand, AuthResponseDto>
{
    private readonly IRepository<User> _userRepository = userRepository;
    private readonly IRepository<Domain.Entities.Employer> _employerRepository = employerRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ISecretHashService _secretHashService = secretHashService;
    private readonly ITokenService _tokenService = tokenService;
    private readonly ILogger<UpdateUserRoleCommandHandler> _logger = logger;

    public async Task<AuthResponseDto> Handle(UpdateUserRoleCommand request, CancellationToken cancellationToken)
    {
        var requestedRole = _tokenService.ParseRole(request.UserRole);
        if (requestedRole is null)
        {
            throw new FluentValidation.ValidationException("Unsupported role value.");
        }

        var user = await _userRepository.FirstOrDefaultAsync(new UserByIdSpec(request.UserId), cancellationToken);

        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedException("Account is unavailable.");
        }

        user.UserRole = requestedRole;
        if (!string.IsNullOrWhiteSpace(request.Fullname))
        {
            user.Fullname = request.Fullname.Trim();
        }

        if (requestedRole == UserRole.Employer)
        {
            var employer = await _employerRepository.FirstOrDefaultAsync(new EmployerByUserIdSpec(user.Id), cancellationToken);
            var normalizedCompanyName = string.IsNullOrWhiteSpace(request.CompanyName) ? null : request.CompanyName.Trim();

            if (employer is null)
            {
                var fallbackCompanyName = string.IsNullOrWhiteSpace(normalizedCompanyName)
                    ? (string.IsNullOrWhiteSpace(user.Fullname) ? "Employer Company" : user.Fullname.Trim())
                    : normalizedCompanyName;

                var finalCompanyName = fallbackCompanyName.Length > 160 ? fallbackCompanyName[..160] : fallbackCompanyName;

                await _employerRepository.AddAsync(new Domain.Entities.Employer
                {
                    UserId = user.Id,
                    CompanyName = finalCompanyName
                }, cancellationToken);
            }
            else if (!string.IsNullOrWhiteSpace(normalizedCompanyName))
            {
                employer.CompanyName = normalizedCompanyName.Length > 160 ? normalizedCompanyName[..160] : normalizedCompanyName;
            }
        }

        user.LastModifiedOn = DateTimeOffset.UtcNow;
        user.LastModifiedBy = user.Id.ToString();

        var refreshToken = _tokenService.CreateRefreshToken();
        user.RefreshToken = _secretHashService.ComputeSha256(refreshToken);
        user.RefreshTokenExpiryTime = _tokenService.GetRefreshTokenExpiry();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var accessToken = _tokenService.CreateAccessToken(user);

        _logger.LogInformation("User role updated. UserId: {UserId}, Role: {Role}", user.Id, user.UserRole);

        return new AuthResponseDto
        {
            Id = user.Id,
            Fullname = user.Fullname,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            UserRole = user.UserRole?.ToString(),
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            RefreshTokenExpiryTime = user.RefreshTokenExpiryTime ?? DateTimeOffset.UtcNow,
            Message = "Role updated successfully."
        };
    }
}
