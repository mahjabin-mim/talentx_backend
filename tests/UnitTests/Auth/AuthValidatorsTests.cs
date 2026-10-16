using Modules.Auth.GoogleAuth;
using Modules.Auth.ChangePassword;
using Modules.Auth.Login;
using Modules.Auth.Logout;
using Modules.Auth.RefreshToken;
using Modules.Auth.ResendVerificationOtp;
using Modules.Auth.Register;
using Modules.Auth.UpdateUserRole;
using Modules.Auth.VerifyEmail;
using Xunit;

namespace UnitTests.Auth;

public class AuthValidatorsTests
{
    [Fact]
    public void RegisterValidator_ShouldFail_ForWeakPassword()
    {
        var validator = new RegisterUserCommandValidator();
        var result = validator.Validate(new RegisterUserCommand("test@example.com", "weakpass", "User", "Company", "+8801234567890"));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void LoginValidator_ShouldFail_ForInvalidEmail()
    {
        var validator = new LoginUserCommandValidator();
        var result = validator.Validate(new LoginUserCommand("not-an-email", "pass"));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void VerifyEmailValidator_ShouldFail_ForInvalidOtp()
    {
        var validator = new VerifyEmailCommandValidator();
        var result = validator.Validate(new VerifyEmailCommand("test@example.com", "12345x"));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ResendVerificationOtpValidator_ShouldFail_ForInvalidEmail()
    {
        var validator = new ResendVerificationOtpCommandValidator();
        var result = validator.Validate(new ResendVerificationOtpCommand("not-an-email"));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void RefreshValidator_ShouldFail_ForEmptyValues()
    {
        var validator = new RefreshTokenCommandValidator();
        var result = validator.Validate(new RefreshTokenCommand(Guid.Empty, string.Empty));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void GoogleAuthValidator_ShouldFail_ForInvalidEmail()
    {
        var validator = new GoogleAuthCommandValidator();
        var result = validator.Validate(new GoogleAuthCommand("invalid", null, null));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void UpdateUserRoleValidator_ShouldFail_ForUnsupportedRole()
    {
        var validator = new UpdateUserRoleCommandValidator();
        var result = validator.Validate(new UpdateUserRoleCommand(Guid.NewGuid(), "SuperAdmin", null, null));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void LogoutValidator_ShouldFail_ForEmptyUserId()
    {
        var validator = new LogoutUserCommandValidator();
        var result = validator.Validate(new LogoutUserCommand(Guid.Empty));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ChangePasswordValidator_ShouldFail_ForWeakPassword()
    {
        var validator = new ChangePasswordCommandValidator();
        var result = validator.Validate(new ChangePasswordCommand(
            Guid.NewGuid(),
            "CurrentP@ss1",
            "weak"));

        Assert.False(result.IsValid);
    }
}
