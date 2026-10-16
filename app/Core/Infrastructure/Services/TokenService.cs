using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Services;

public sealed class TokenService(IOptions<JwtOptions> jwtOptions) : ITokenService
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public string CreateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Fullname ?? user.Email),
            new Claim("token_version", user.TokenVersion.ToString())
        };

        if (user.UserRole.HasValue)
        {
            claims.Add(new Claim(ClaimTypes.Role, user.UserRole.Value.ToString()));
        }

        if (!string.IsNullOrWhiteSpace(user.PhoneNumber))
        {
            claims.Add(new Claim("phone_number", user.PhoneNumber.Trim()));
        }

        var tokenDescriptor = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenExpiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
    }

    public string CreateRefreshToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(randomBytes);
    }

    public DateTimeOffset GetRefreshTokenExpiry()
        => DateTimeOffset.UtcNow.AddDays(_jwtOptions.RefreshTokenExpiryDays);

    public DateTimeOffset GetEmailVerificationCodeExpiry()
        => DateTimeOffset.UtcNow.AddMinutes(15);

    public string CreateEmailVerificationCode()
        => RandomNumberGenerator.GetInt32(100000, 999999).ToString();

    public UserRole? ParseRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return null;
        }

        return Enum.TryParse<UserRole>(role, true, out var parsedRole)
            ? parsedRole
            : null;
    }
}
