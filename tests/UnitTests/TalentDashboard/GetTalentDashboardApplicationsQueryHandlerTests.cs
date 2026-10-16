using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Infrastructure.Data;
using Infrastructure.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Modules.TalentDashboard.GetApplications;
using Moq;
using Shared.Exceptions;
using Xunit;

namespace UnitTests.TalentDashboard;

public sealed class GetTalentDashboardApplicationsQueryHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IRepository<Application>> _applicationRepositoryMock = new();

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task Handle_ShouldReturnApplications_ForActiveTalentUser()
    {
        var userId = Guid.NewGuid();
        var employerId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var applicationId = Guid.NewGuid();

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User
            {
                Id = userId,
                IsActive = true,
                UserRole = UserRole.Talent
            });

        var applications = new List<Application>
        {
            new()
            {
                Id = applicationId,
                CreatedAt = new DateTimeOffset(2026, 4, 28, 8, 0, 0, TimeSpan.Zero),
                Status = ApplicationStatus.Shortlisted,
                Job = new Job
                {
                    Id = jobId,
                    Title = "Frontend Developer",
                    CurrencyCode = "BDT",
                    SalaryMin = 40000,
                    SalaryMax = 60000,
                    WorkArrangement = WorkArrangement.Onsite,
                    City = "Dhaka",
                    CountryCode = "BD",
                    Timezone = "Asia/Dhaka",
                    Employer = new Employer
                    {
                        Id = employerId,
                        CompanyName = "Tech Corp"
                    }
                }
            }
        };

        _applicationRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<Domain.Specifications.TalentDashboardApplicationsByUserIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(applications);

        await using var dbContext = CreateDbContext();
        dbContext.AiScores.Add(new AiScore
        {
            JobId = jobId,
            TalentId = Guid.NewGuid(),
            Score = 82m,
            CalculatedAt = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var handler = new GetTalentDashboardApplicationsQueryHandler(
            _userRepositoryMock.Object,
            _applicationRepositoryMock.Object,
            dbContext,
            Options.Create(new AiScoringOptions()));

        var result = await handler.Handle(
            new GetTalentDashboardApplicationsQuery(userId, 1, 1, 5),
            CancellationToken.None);

        Assert.Single(result.Applications);
        var item = result.Applications[0];
        Assert.Equal(applicationId, item.Id);
        Assert.Equal("Frontend Developer", item.JobTitle);
        Assert.Equal("Tech Corp", item.Organization);
        Assert.Equal("Shortlisted", item.Status);
        Assert.Equal($"/jobs?jobId={jobId}", item.JobLink);
        Assert.Equal("BDT 40000 - 60000", item.SalaryRange);
        Assert.Equal("Dhaka, BD", item.Location);
        Assert.Equal(1, result.ApplicationsPagination.Page);
        Assert.Equal(5, result.ApplicationsPagination.PageSize);
        Assert.Equal(1, result.ApplicationsPagination.TotalItems);
        Assert.Equal(1, result.ApplicationsPagination.TotalPages);
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorized_WhenUserIsNotTalent()
    {
        var userId = Guid.NewGuid();

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User
            {
                Id = userId,
                IsActive = true,
                UserRole = UserRole.Employer
            });

        await using var dbContext = CreateDbContext();

        var handler = new GetTalentDashboardApplicationsQueryHandler(
            _userRepositoryMock.Object,
            _applicationRepositoryMock.Object,
            dbContext,
            Options.Create(new AiScoringOptions()));

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(
                new GetTalentDashboardApplicationsQuery(userId, 1, 1, 5),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldReturnDerivedInvitations_ForMatchedTalentProfile()
    {
        var userId = Guid.NewGuid();
        var employerId = Guid.NewGuid();
        var employerUserId = Guid.NewGuid();
        var talentProfileId = Guid.NewGuid();
        var jobId = Guid.NewGuid();

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User
            {
                Id = userId,
                IsActive = true,
                UserRole = UserRole.Talent
            });

        _applicationRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<Domain.Specifications.TalentDashboardApplicationsByUserIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await using var dbContext = CreateDbContext();
        dbContext.Users.AddRange(
            new User
            {
                Id = userId,
                IsActive = true,
                UserRole = UserRole.Talent
            },
            new User
            {
                Id = employerUserId,
                IsActive = true,
                UserRole = UserRole.Employer
            });
        dbContext.TalentProfiles.Add(new TalentProfile
        {
            Id = talentProfileId,
            UserId = userId,
            CreatedAt = DateTimeOffset.UtcNow
        });
        dbContext.Jobs.Add(new Job
        {
            Id = jobId,
            EmployerId = employerId,
            Title = "Backend Engineer",
            Description = "Build APIs",
            Responsibilities = "Ship backend features",
            EmploymentType = EmploymentType.FullTime,
            WorkArrangement = WorkArrangement.Remote,
            ExperienceLevel = ExperienceLevel.MidLevel,
            Status = JobStatus.Active,
            ExperienceMinYears = 2,
            ExperienceMaxYears = 5,
            SalaryMin = 50000,
            SalaryMax = 80000,
            CountryCode = "BD",
            City = "Dhaka",
            Timezone = "Asia/Dhaka",
            CurrencyCode = "BDT",
            NumberOfPositions = 1,
            ApplicationDeadline = DateTimeOffset.UtcNow.AddDays(14),
            CreatedOn = DateTimeOffset.UtcNow,
            Employer = new Employer
            {
                Id = employerId,
                UserId = employerUserId,
                CompanyName = "Nova Labs"
            }
        });
        dbContext.AiScores.Add(new AiScore
        {
            JobId = jobId,
            TalentId = talentProfileId,
            Score = 84m,
            CalculatedAt = new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero)
        });
        await dbContext.SaveChangesAsync();

        var handler = new GetTalentDashboardApplicationsQueryHandler(
            _userRepositoryMock.Object,
            _applicationRepositoryMock.Object,
            dbContext,
            Options.Create(new AiScoringOptions
            {
                InvitationMatchThreshold = 70m
            }));

        var result = await handler.Handle(
            new GetTalentDashboardApplicationsQuery(userId, 1, 1, 5),
            CancellationToken.None);

        Assert.Empty(result.Applications);
        Assert.Single(result.Invitations);
        var invitation = result.Invitations[0];
        Assert.Equal("Backend Engineer", invitation.JobTitle);
        Assert.Equal("Nova Labs", invitation.Organization);
        Assert.Equal("Pending", invitation.Status);
        Assert.Equal("Full Time", invitation.EmploymentType);
        Assert.Equal($"/jobs?jobId={jobId}", invitation.JobLink);
    }
}
