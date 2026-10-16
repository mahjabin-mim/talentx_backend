using Domain.Entities;
using Domain.Enums;

namespace Infrastructure.Services;

public interface ITokenService
{
    string CreateAccessToken(User user);
    string CreateRefreshToken();
    DateTimeOffset GetRefreshTokenExpiry();
    DateTimeOffset GetEmailVerificationCodeExpiry();
    string CreateEmailVerificationCode();
    UserRole? ParseRole(string? role);
}
