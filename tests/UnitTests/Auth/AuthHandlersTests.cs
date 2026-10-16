using Ardalis.Specification;
using System.IdentityModel.Tokens.Jwt;
using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Infrastructure.Options;
using Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Modules.Auth.ChangePassword;
using Modules.Auth.GoogleAuth;
using Modules.Auth.Login;
using Modules.Auth.Logout;
using Modules.Auth.RefreshToken;
using Modules.Auth.ResendVerificationOtp;
using Modules.Auth.Register;
using Modules.Auth.UpdateUserRole;
using Modules.Auth.VerifyEmail;
using Moq;
using Shared.Exceptions;
using Shared.Services;
using Xunit;

namespace UnitTests.Auth;

public class AuthHandlersTests
{
    [Fact]
    public async Task RegisterHandler_ShouldCreateUser()
    {
        var userRepositoryMock = new Mock<IRepository<User>>();
        var employerRepositoryMock = new Mock<IRepository<Employer>>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();

        userRepositoryMock.Setup(x => x.AnyAsync(It.IsAny<ISpecification<User>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var tokenService = new FakeTokenService();
        IPasswordService passwordService = new PasswordService();
        ISecretHashService hashService = new SecretHashService();
        var authEmailServiceMock = new Mock<IAuthEmailService>();

        var handler = new RegisterUserCommandHandler(
            userRepositoryMock.Object,
            employerRepositoryMock.Object,
            unitOfWorkMock.Object,
            passwordService,
            hashService,
            tokenService,
            authEmailServiceMock.Object,
            NullLogger<RegisterUserCommandHandler>.Instance);

        var response = await handler.Handle(
            new RegisterUserCommand("user@example.com", "StrongP@ss1", "User", "Company", "+8801234567890"),
            CancellationToken.None);

        Assert.True(response.RequiresVerification);
        Assert.Equal("verify_email", response.NextAction);
        userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        employerRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Employer>(), It.IsAny<CancellationToken>()), Times.Once);
        unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        authEmailServiceMock.Verify(x => x.SendEmailVerificationOtpAsync(
            "user@example.com",
            "User",
            "123456",
            response.VerificationCodeExpiresAtUtc,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task VerifyEmailHandler_ShouldConfirmUser()
    {
        var userRepositoryMock = new Mock<IRepository<User>>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        ISecretHashService hashService = new SecretHashService();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            Password = "hashed",
            Fullname = "User",
            IsActive = true,
            EmailConfirmed = false,
            EmailVerificationCode = hashService.ComputeSha256("123456"),
            EmailVerificationCodeExpiryTime = DateTimeOffset.UtcNow.AddMinutes(10),
            CreatedOn = DateTimeOffset.UtcNow
        };

        userRepositoryMock.Setup(x => x.FirstOrDefaultAsync(It.IsAny<ISpecification<User>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = new VerifyEmailCommandHandler(
            userRepositoryMock.Object,
            unitOfWorkMock.Object,
            hashService,
            NullLogger<VerifyEmailCommandHandler>.Instance);

        var response = await handler.Handle(
            new VerifyEmailCommand("user@example.com", "123456"),
            CancellationToken.None);

        Assert.True(response.Verified);
        Assert.Equal("login", response.NextAction);
        Assert.True(user.EmailConfirmed);
        Assert.Null(user.EmailVerificationCode);
        Assert.Null(user.EmailVerificationCodeExpiryTime);
        unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task VerifyEmailHandler_ShouldReturnOtpCodeValidationError_ForInvalidCode()
    {
        var userRepositoryMock = new Mock<IRepository<User>>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        ISecretHashService hashService = new SecretHashService();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            Password = "hashed",
            IsActive = true,
            EmailConfirmed = false,
            EmailVerificationCode = hashService.ComputeSha256("123456"),
            EmailVerificationCodeExpiryTime = DateTimeOffset.UtcNow.AddMinutes(10),
            CreatedOn = DateTimeOffset.UtcNow
        };

        userRepositoryMock.Setup(x => x.FirstOrDefaultAsync(It.IsAny<ISpecification<User>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = new VerifyEmailCommandHandler(
            userRepositoryMock.Object,
            unitOfWorkMock.Object,
            hashService,
            NullLogger<VerifyEmailCommandHandler>.Instance);

        var exception = await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            handler.Handle(new VerifyEmailCommand("user@example.com", "000000"), CancellationToken.None));

        Assert.Contains(exception.Errors, x =>
            x.PropertyName == nameof(VerifyEmailCommand.OtpCode)
            && x.ErrorMessage == "Verification code is invalid or expired.");
    }

    [Fact]
    public async Task ResendVerificationOtpHandler_ShouldUpdateCodeAndSendEmail()
    {
        var userRepositoryMock = new Mock<IRepository<User>>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var authEmailServiceMock = new Mock<IAuthEmailService>();
        ISecretHashService hashService = new SecretHashService();
        var tokenService = new FakeTokenService();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            Password = "hashed",
            Fullname = "User",
            IsActive = true,
            EmailConfirmed = false,
            CreatedOn = DateTimeOffset.UtcNow
        };

        userRepositoryMock.Setup(x => x.FirstOrDefaultAsync(It.IsAny<ISpecification<User>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = new ResendVerificationOtpCommandHandler(
            userRepositoryMock.Object,
            unitOfWorkMock.Object,
            hashService,
            tokenService,
            authEmailServiceMock.Object,
            NullLogger<ResendVerificationOtpCommandHandler>.Instance);

        var response = await handler.Handle(
            new ResendVerificationOtpCommand("user@example.com"),
            CancellationToken.None);

        Assert.True(response.RequiresVerification);
        Assert.Equal("verify_email", response.NextAction);
        Assert.Equal(hashService.ComputeSha256("123456"), user.EmailVerificationCode);
        Assert.NotNull(user.EmailVerificationCodeExpiryTime);
        unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        authEmailServiceMock.Verify(x => x.SendEmailVerificationOtpAsync(
            "user@example.com",
            "User",
            "123456",
            response.VerificationCodeExpiresAtUtc,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResendVerificationOtpHandler_ShouldReturnEmailValidationError_WhenAlreadyVerified()
    {
        var userRepositoryMock = new Mock<IRepository<User>>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var authEmailServiceMock = new Mock<IAuthEmailService>();
        ISecretHashService hashService = new SecretHashService();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            Password = "hashed",
            IsActive = true,
            EmailConfirmed = true,
            CreatedOn = DateTimeOffset.UtcNow
        };

        userRepositoryMock.Setup(x => x.FirstOrDefaultAsync(It.IsAny<ISpecification<User>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = new ResendVerificationOtpCommandHandler(
            userRepositoryMock.Object,
            unitOfWorkMock.Object,
            hashService,
            new FakeTokenService(),
            authEmailServiceMock.Object,
            NullLogger<ResendVerificationOtpCommandHandler>.Instance);

        var exception = await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            handler.Handle(new ResendVerificationOtpCommand("user@example.com"), CancellationToken.None));

        Assert.Contains(exception.Errors, x =>
            x.PropertyName == nameof(ResendVerificationOtpCommand.Email)
            && x.ErrorMessage == "Email is already verified.");
    }

    [Fact]
    public async Task LoginHandler_ShouldThrow_ForWrongPassword()
    {
        var userRepositoryMock = new Mock<IRepository<User>>();
        var applicationRepositoryMock = new Mock<IRepository<Application>>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();

        IPasswordService passwordService = new PasswordService();
        ISecretHashService hashService = new SecretHashService();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            Password = passwordService.HashPassword("CorrectP@ss1"),
            IsActive = true,
            CreatedOn = DateTimeOffset.UtcNow
        };

        userRepositoryMock.Setup(x => x.FirstOrDefaultAsync(It.IsAny<ISpecification<User>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = new LoginUserCommandHandler(
            userRepositoryMock.Object,
            applicationRepositoryMock.Object,
            unitOfWorkMock.Object,
            passwordService,
            hashService,
            new FakeTokenService(),
            NullLogger<LoginUserCommandHandler>.Instance);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(new LoginUserCommand("user@example.com", "wrong"), CancellationToken.None));
    }

    [Fact]
    public async Task LoginHandler_ShouldRequireEmailVerification_WhenEmailNotConfirmed()
    {
        var userRepositoryMock = new Mock<IRepository<User>>();
        var applicationRepositoryMock = new Mock<IRepository<Application>>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();

        IPasswordService passwordService = new PasswordService();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            Password = passwordService.HashPassword("CorrectP@ss1"),
            IsActive = true,
            EmailConfirmed = false,
            CreatedOn = DateTimeOffset.UtcNow
        };

        userRepositoryMock.Setup(x => x.FirstOrDefaultAsync(It.IsAny<ISpecification<User>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = new LoginUserCommandHandler(
            userRepositoryMock.Object,
            applicationRepositoryMock.Object,
            unitOfWorkMock.Object,
            passwordService,
            new SecretHashService(),
            new FakeTokenService(),
            NullLogger<LoginUserCommandHandler>.Instance);

        await Assert.ThrowsAsync<EmailVerificationRequiredException>(() =>
            handler.Handle(new LoginUserCommand("user@example.com", "CorrectP@ss1"), CancellationToken.None));
    }

    [Fact]
    public async Task LoginHandler_ShouldReturnPhoneNumber()
    {
        var userRepositoryMock = new Mock<IRepository<User>>();
        var applicationRepositoryMock = new Mock<IRepository<Application>>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();

        IPasswordService passwordService = new PasswordService();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            PhoneNumber = "+8801234567890",
            Password = passwordService.HashPassword("CorrectP@ss1"),
            IsActive = true,
            EmailConfirmed = true,
            CreatedOn = DateTimeOffset.UtcNow
        };

        userRepositoryMock.Setup(x => x.FirstOrDefaultAsync(It.IsAny<ISpecification<User>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = new LoginUserCommandHandler(
            userRepositoryMock.Object,
            applicationRepositoryMock.Object,
            unitOfWorkMock.Object,
            passwordService,
            new SecretHashService(),
            new FakeTokenService(),
            NullLogger<LoginUserCommandHandler>.Instance);

        var response = await handler.Handle(
            new LoginUserCommand("user@example.com", "CorrectP@ss1"),
            CancellationToken.None);

        Assert.Equal("+8801234567890", response.PhoneNumber);
    }

    [Fact]
    public void TokenService_ShouldIncludePhoneNumberClaim()
    {
        var service = new TokenService(Options.Create(new JwtOptions
        {
            Issuer = "issuer",
            Audience = "audience",
            Key = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
            AccessTokenExpiryMinutes = 15,
            RefreshTokenExpiryDays = 30
        }));

        var token = service.CreateAccessToken(new User
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            Fullname = "User",
            PhoneNumber = "+8801234567890",
            TokenVersion = 0
        });

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Contains(jwt.Claims, x => x.Type == "phone_number" && x.Value == "+8801234567890");
    }

    [Fact]
    public async Task RefreshHandler_ShouldRotateRefreshToken()
    {
        var userRepositoryMock = new Mock<IRepository<User>>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();

        ISecretHashService hashService = new SecretHashService();
        var incomingToken = "incoming-refresh";

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            Password = "hashed",
            IsActive = true,
            RefreshToken = hashService.ComputeSha256(incomingToken),
            RefreshTokenExpiryTime = DateTimeOffset.UtcNow.AddMinutes(10),
            CreatedOn = DateTimeOffset.UtcNow
        };

        userRepositoryMock.Setup(x => x.FirstOrDefaultAsync(It.IsAny<ISpecification<User>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = new RefreshTokenCommandHandler(
            userRepositoryMock.Object,
            unitOfWorkMock.Object,
            hashService,
            new FakeTokenService(),
            NullLogger<RefreshTokenCommandHandler>.Instance);

        var response = await handler.Handle(
            new RefreshTokenCommand(user.Id, incomingToken),
            CancellationToken.None);

        Assert.Equal(user.Id, response.Id);
        Assert.NotEqual(hashService.ComputeSha256(incomingToken), user.RefreshToken);
        unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LogoutHandler_ShouldIncrementTokenVersion()
    {
        var userRepositoryMock = new Mock<IRepository<User>>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            Password = "hashed",
            IsActive = true,
            TokenVersion = 2,
            RefreshToken = "x",
            RefreshTokenExpiryTime = DateTimeOffset.UtcNow.AddDays(1),
            CreatedOn = DateTimeOffset.UtcNow
        };

        userRepositoryMock.Setup(x => x.FirstOrDefaultAsync(It.IsAny<ISpecification<User>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = new LogoutUserCommandHandler(
            userRepositoryMock.Object,
            unitOfWorkMock.Object,
            NullLogger<LogoutUserCommandHandler>.Instance);

        await handler.Handle(new LogoutUserCommand(user.Id), CancellationToken.None);

        Assert.Equal(3, user.TokenVersion);
        Assert.Null(user.RefreshToken);
        unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GoogleHandler_ShouldCreateUser_WhenMissing()
    {
        var userRepositoryMock = new Mock<IRepository<User>>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();

        userRepositoryMock.Setup(x => x.FirstOrDefaultAsync(It.IsAny<ISpecification<User>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var handler = new GoogleAuthCommandHandler(
            userRepositoryMock.Object,
            unitOfWorkMock.Object,
            new SecretHashService(),
            new FakeTokenService(),
            NullLogger<GoogleAuthCommandHandler>.Instance);

        var response = await handler.Handle(
            new GoogleAuthCommand("google@example.com", "Google User", null),
            CancellationToken.None);

        Assert.Equal("google@example.com", response.Email);
        userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateRoleHandler_ShouldSetRole()
    {
        var userRepositoryMock = new Mock<IRepository<User>>();
        var employerRepositoryMock = new Mock<IRepository<Employer>>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "role@example.com",
            Password = "hashed",
            IsActive = true,
            CreatedOn = DateTimeOffset.UtcNow
        };

        userRepositoryMock.Setup(x => x.FirstOrDefaultAsync(It.IsAny<ISpecification<User>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        employerRepositoryMock.Setup(x => x.FirstOrDefaultAsync(It.IsAny<ISpecification<Employer>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employer?)null);

        var handler = new UpdateUserRoleCommandHandler(
            userRepositoryMock.Object,
            employerRepositoryMock.Object,
            unitOfWorkMock.Object,
            new SecretHashService(),
            new FakeTokenService(),
            NullLogger<UpdateUserRoleCommandHandler>.Instance);

        var response = await handler.Handle(
            new UpdateUserRoleCommand(user.Id, "Employer", null, null),
            CancellationToken.None);

        Assert.Equal(UserRole.Employer, user.UserRole);
        Assert.Equal("Employer", response.UserRole);
        employerRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Employer>(), It.IsAny<CancellationToken>()), Times.Once);
        unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ChangePasswordHandler_ShouldThrow_ForIncorrectCurrentPassword()
    {
        var userRepositoryMock = new Mock<IRepository<User>>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        IPasswordService passwordService = new PasswordService();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            Password = passwordService.HashPassword("CorrectP@ss1"),
            IsActive = true,
            CreatedOn = DateTimeOffset.UtcNow
        };

        userRepositoryMock.Setup(x => x.FirstOrDefaultAsync(It.IsAny<ISpecification<User>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = new ChangePasswordCommandHandler(
            userRepositoryMock.Object,
            unitOfWorkMock.Object,
            passwordService);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(
                new ChangePasswordCommand(user.Id, "WrongP@ss1", "NewP@ssword1!"),
                CancellationToken.None));
    }

    [Fact]
    public async Task ChangePasswordHandler_ShouldUpdatePassword_AndInvalidateSessions()
    {
        var userRepositoryMock = new Mock<IRepository<User>>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        IPasswordService passwordService = new PasswordService();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            Password = passwordService.HashPassword("CorrectP@ss1"),
            IsActive = true,
            TokenVersion = 5,
            RefreshToken = "x",
            RefreshTokenExpiryTime = DateTimeOffset.UtcNow.AddDays(7),
            PreviousRefreshToken = "y",
            PreviousRefreshTokenExpiryTime = DateTimeOffset.UtcNow.AddDays(1),
            CreatedOn = DateTimeOffset.UtcNow
        };

        userRepositoryMock.Setup(x => x.FirstOrDefaultAsync(It.IsAny<ISpecification<User>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = new ChangePasswordCommandHandler(
            userRepositoryMock.Object,
            unitOfWorkMock.Object,
            passwordService);

        await handler.Handle(
            new ChangePasswordCommand(user.Id, "CorrectP@ss1", "NewP@ssword1!"),
            CancellationToken.None);

        Assert.True(passwordService.VerifyPassword("NewP@ssword1!", user.Password));
        Assert.Equal(6, user.TokenVersion);
        Assert.Null(user.RefreshToken);
        Assert.Null(user.RefreshTokenExpiryTime);
        Assert.Null(user.PreviousRefreshToken);
        Assert.Null(user.PreviousRefreshTokenExpiryTime);
        unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
