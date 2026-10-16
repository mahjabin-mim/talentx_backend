using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Shared.Exceptions;

namespace Infrastructure.Services;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public Guid GetRequiredUserId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        var userIdClaim = user?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user?.FindFirstValue("sub");

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedException("Invalid user context.");
        }

        return userId;
    }
}
