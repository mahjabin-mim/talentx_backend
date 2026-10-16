using Ardalis.Specification;
using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Services;
using Infrastructure.Services.AiScoring;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Modules.Applications.Create;
using Moq;
using Xunit;

namespace UnitTests.Applications;

public sealed class CreateApplicationCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IRepository<Application>> _applicationRepositoryMock = new();
    private readonly Mock<IRepository<Job>> _jobRepositoryMock = new();
    private readonly Mock<IRepository<TalentProfile>> _talentProfileRepositoryMock = new();
    private readonly Mock<IApplicationAccountEmailService> _applicationAccountEmailServiceMock = new();
    private readonly Mock<IPasswordService> _passwordServiceMock = new();
    private readonly Mock<IResumeStorageService> _resumeStorageServiceMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IAiScoreInvalidationService> _aiScoreInvalidationServiceMock = new();

    [Fact]
    public async Task Handle_ShouldCreateUserProfileAndApplication_WhenUserDoesNotExist()
    {
        var jobId = Guid.NewGuid();
        var createdUser = (User?)null;
        var createdTalentProfile = (TalentProfile?)null;
        var createdApplication = (Application?)null;

        _jobRepositoryMock
            .Setup(x => x.AnyAsync(It.IsAny<ISpecification<Job>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync("candidate@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _passwordServiceMock
            .Setup(x => x.HashPassword(It.IsAny<string>()))
            .Returns("hashed-password");
        _userRepositoryMock
            .Setup(x => x.Add(It.IsAny<User>()))
            .Callback<User>(user =>
            {
                user.Id = Guid.NewGuid();
                createdUser = user;
            });
        _talentProfileRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ISpecification<TalentProfile>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TalentProfile?)null);
        _talentProfileRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<TalentProfile>(), It.IsAny<CancellationToken>()))
            .Callback<TalentProfile, CancellationToken>((profile, _) => createdTalentProfile = profile)
            .Returns(Task.CompletedTask);
        _applicationRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Application>(), It.IsAny<CancellationToken>()))
            .Callback<Application, CancellationToken>((application, _) => createdApplication = application)
            .Returns(Task.CompletedTask);
        _resumeStorageServiceMock
            .Setup(x => x.UploadAsync(
                It.IsAny<Stream>(),
                "resume.pdf",
                "application/pdf",
                It.IsAny<CancellationToken>(),
                null))
            .ReturnsAsync(new ResumeUploadResult
            {
                ObjectKey = "resumes/resume.pdf",
                ObjectUrl = "https://cdn.example.com/resumes/resume.pdf"
            });

        var handler = CreateHandler();
        var command = CreateCommand(jobId, "Candidate@Example.com");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.ExistingUserFound);
        Assert.NotNull(createdUser);
        Assert.Equal("candidate@example.com", createdUser!.Email);
        Assert.Equal("hashed-password", createdUser.Password);

        Assert.NotNull(createdTalentProfile);
        Assert.Equal(createdUser.Id, createdTalentProfile!.UserId);
        Assert.Equal("https://cdn.example.com/resumes/resume.pdf", createdTalentProfile.ResumeUrl);

        Assert.NotNull(createdApplication);
        Assert.Equal(jobId, createdApplication!.JobId);
        Assert.Equal(createdTalentProfile.Id, createdApplication.TalentId);
        Assert.Equal("candidate@example.com", createdApplication.Email);

        _applicationAccountEmailServiceMock.Verify(
            x => x.SendNewAccountPasswordAsync(
                "candidate@example.com",
                "Mahjabin Mim",
                It.IsAny<string>()),
            Times.Once);
        _aiScoreInvalidationServiceMock.Verify(
            x => x.EnqueueScoreForPairAsync(
                jobId,
                createdTalentProfile.Id,
                "application-submitted",
                It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task Handle_ShouldSkipUserCreation_WhenUserAlreadyExists()
    {
        var jobId = Guid.NewGuid();
        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "candidate@example.com",
            Fullname = "Existing Candidate"
        };
        var existingProfile = new TalentProfile
        {
            Id = Guid.NewGuid(),
            UserId = existingUser.Id
        };

        _jobRepositoryMock
            .Setup(x => x.AnyAsync(It.IsAny<ISpecification<Job>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync("candidate@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);
        _talentProfileRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ISpecification<TalentProfile>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProfile);
        _applicationRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Application>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _resumeStorageServiceMock
            .Setup(x => x.UploadAsync(
                It.IsAny<Stream>(),
                "resume.pdf",
                "application/pdf",
                It.IsAny<CancellationToken>(),
                null))
            .ReturnsAsync(new ResumeUploadResult
            {
                ObjectKey = "resumes/resume.pdf",
                ObjectUrl = "https://cdn.example.com/resumes/resume.pdf"
            });

        var handler = CreateHandler();
        var command = CreateCommand(jobId, "candidate@example.com");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.ExistingUserFound);
        _userRepositoryMock.Verify(x => x.Add(It.IsAny<User>()), Times.Never);
        _applicationAccountEmailServiceMock.Verify(
            x => x.SendNewAccountPasswordAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>()),
            Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private CreateApplicationCommandHandler CreateHandler()
    {
        return new CreateApplicationCommandHandler(
            _userRepositoryMock.Object,
            _applicationRepositoryMock.Object,
            _jobRepositoryMock.Object,
            _talentProfileRepositoryMock.Object,
            _applicationAccountEmailServiceMock.Object,
            _passwordServiceMock.Object,
            _resumeStorageServiceMock.Object,
            _unitOfWorkMock.Object,
            _aiScoreInvalidationServiceMock.Object,
            NullLogger<CreateApplicationCommandHandler>.Instance);
    }

    private static CreateApplicationCommand CreateCommand(Guid jobId, string email)
    {
        return new CreateApplicationCommand(
            jobId,
            "Mahjabin Mim",
            email,
            "+8801860954446",
            "BSc in CSE",
            "CSE",
            "3.80",
            "https://www.linkedin.com/in/mahjabin",
            "https://github.com/mahjabin",
            "Dhaka, Bangladesh",
            "I am interested in this opportunity.",
            60000,
            2,
            BuildPdfFormFile());
    }

    private static IFormFile BuildPdfFormFile()
    {
        var bytes = new byte[] { 37, 80, 68, 70 };
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, stream.Length, "Resume", "resume.pdf")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };
    }
}
