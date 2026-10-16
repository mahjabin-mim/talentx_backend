using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace UnitTests.Auth;

internal static class TestUtilities
{
    public static AppDbContext CreateDbContext(string? name = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(name ?? Guid.NewGuid().ToString("N"))
            .Options;

        return new AppDbContext(options);
    }
}

internal sealed class FakeTokenService : ITokenService
{
    public string CreateAccessToken(User user) => $"access-{user.Id}";

    public string CreateRefreshToken() => "refresh-token";

    public DateTimeOffset GetRefreshTokenExpiry() => DateTimeOffset.UtcNow.AddDays(30);

    public DateTimeOffset GetEmailVerificationCodeExpiry() => DateTimeOffset.UtcNow.AddMinutes(15);

    public string CreateEmailVerificationCode() => "123456";

    public UserRole? ParseRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return null;
        }

        return Enum.TryParse<UserRole>(role, true, out var parsed)
            ? parsed
            : null;
    }
}
