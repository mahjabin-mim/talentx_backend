using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IntegrationTests;

public sealed class AuthEndpointsIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public AuthEndpointsIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ShouldReturnCreated_WithVerificationPayload()
    {
        var payload = new
        {
            email = $"integration+{Guid.NewGuid():N}@example.com",
            password = "StrongP@ss1",
            fullname = "Integration User",
            companyName = "Integration Company",
            phoneNumber = "+8801234567890"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.NotNull(body);
        Assert.Equal("Registration completed. Please verify your email with the OTP code.", body!.Message);
        Assert.True(body.RequiresVerification);
        Assert.Equal("verify_email", body.NextAction);
        Assert.True(body.VerificationCodeExpiresAtUtc > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task ResendVerificationOtp_ShouldReturnOk_ForPendingAccount()
    {
        var email = $"integration-resend+{Guid.NewGuid():N}@example.com";
        var password = "StrongP@ss1";

        var register = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password,
            fullname = "Integration User",
            companyName = "Integration Company",
            phoneNumber = "+8801234567890"
        });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        var resend = await _client.PostAsJsonAsync("/api/v1/auth/resend-verification-otp", new
        {
            email
        });

        Assert.Equal(HttpStatusCode.OK, resend.StatusCode);

        var body = await resend.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.NotNull(body);
        Assert.True(body!.RequiresVerification);
        Assert.Equal("verify_email", body.NextAction);
        Assert.True(body.VerificationCodeExpiresAtUtc > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task VerifyEmail_ShouldReturnBadRequest_ForInvalidOtp()
    {
        var email = $"integration-verify+{Guid.NewGuid():N}@example.com";
        var password = "StrongP@ss1";

        var register = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password,
            fullname = "Integration User",
            companyName = "Integration Company",
            phoneNumber = "+8801234567890"
        });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        var verify = await _client.PostAsJsonAsync("/api/v1/auth/verify-email", new
        {
            email,
            otpCode = "000000"
        });

        Assert.Equal(HttpStatusCode.BadRequest, verify.StatusCode);
    }

    [Fact]
    public async Task Login_Refresh_Logout_RefreshAgain_ShouldFailAfterLogout()
    {
        var email = $"integration-flow+{Guid.NewGuid():N}@example.com";
        var password = "StrongP@ss1";

        var register = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password,
            fullname = "Integration User",
            companyName = "Integration Company",
            phoneNumber = "+8801234567890"
        });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        await ConfirmEmailAsync(email);

        var login = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email,
            password
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var loginBody = await login.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(loginBody);

        var refresh = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            userId = loginBody!.Id,
            refreshToken = loginBody.RefreshToken
        });
        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);

        var refreshBody = await refresh.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(refreshBody);

        using var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        logoutRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshBody!.AccessToken);

        var logout = await _client.SendAsync(logoutRequest);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        var refreshAfterLogout = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            userId = refreshBody.Id,
            refreshToken = refreshBody.RefreshToken
        });

        Assert.Equal(HttpStatusCode.Unauthorized, refreshAfterLogout.StatusCode);
    }

    [Fact]
    public async Task Login_ShouldReturnForbidden_WithVerificationAction_WhenEmailIsNotVerified()
    {
        var email = $"integration-login-unverified+{Guid.NewGuid():N}@example.com";
        var password = "StrongP@ss1";

        var register = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password,
            fullname = "Integration User",
            companyName = "Integration Company",
            phoneNumber = "+8801234567890"
        });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        var login = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email,
            password
        });

        Assert.Equal(HttpStatusCode.Forbidden, login.StatusCode);

        var body = await login.Content.ReadFromJsonAsync<AuthErrorResponse>();
        Assert.NotNull(body);
        Assert.True(body!.RequiresVerification);
        Assert.Equal("verify_email", body.NextAction);
    }

    private async Task ConfirmEmailAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = dbContext.Users.Single(x => x.Email == email);
        user.EmailConfirmed = true;
        user.EmailVerificationCode = null;
        user.EmailVerificationCodeExpiryTime = null;
        await dbContext.SaveChangesAsync();
    }

    private sealed class AuthResponse
    {
        public Guid Id { get; init; }
        public string AccessToken { get; init; } = string.Empty;
        public string RefreshToken { get; init; } = string.Empty;
    }

    private sealed class RegisterResponse
    {
        public string Message { get; init; } = string.Empty;
        public bool RequiresVerification { get; init; }
        public string NextAction { get; init; } = string.Empty;
        public DateTimeOffset VerificationCodeExpiresAtUtc { get; init; }
    }

    private sealed class AuthErrorResponse
    {
        public bool? RequiresVerification { get; init; }
        public string? NextAction { get; init; }
    }
}
