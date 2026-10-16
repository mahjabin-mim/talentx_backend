using Infrastructure.Services.EmailTemplates;
using Xunit;

namespace UnitTests.Auth;

public sealed class AuthEmailTemplateTests
{
    [Fact]
    public void EmailVerificationOtpTemplate_ShouldRender()
    {
        var renderer = new EmailTemplateRenderer();

        var rendered = renderer.Render(
            "Auth.EmailVerificationOtp",
            new Dictionary<string, string>
            {
                ["FullName"] = "Test User",
                ["OtpCode"] = "123456",
                ["ExpiresAtUtc"] = "2026-04-28 12:34:56 UTC"
            });

        Assert.Equal("Verify your TalentX email", rendered.Subject);
        Assert.Contains("Test User", rendered.Body);
        Assert.Contains("123456", rendered.Body);
        Assert.Contains("2026-04-28 12:34:56 UTC", rendered.Body);
    }
}
