using Ardalis.Specification;
using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using FluentValidation;
using Infrastructure.Services.AiScoring;
using Microsoft.Extensions.Logging.Abstractions;
using Modules.Jobs.CreateJob;
using Modules.Jobs.DeleteJob;
using Modules.Jobs.GetEmployerDashboard;
using Modules.Jobs.GetJobById;
using Modules.Jobs.GetPublicJobs;
using Modules.Jobs.UpdateJob;
using Modules.Jobs.UpdateJobStatus;
using Moq;
using Shared.Exceptions;
using Xunit;

namespace UnitTests.Jobs;

public class JobHandlersTests
{
    // Old repository mocks (unused, kept for reference)
    // private readonly Mock<IUserRepository> _userRepoMock = new();
    // private readonly Mock<IEmployerRepository> _employerRepoMock = new();
    // private readonly Mock<IJobRepository> _jobRepoMock = new();
    // Use generic repository mocks for new handler signatures
    private readonly Mock<IRepository<User>> _genericUserRepoMock = new();
    private readonly Mock<IRepository<Employer>> _genericEmployerRepoMock = new();
    private readonly Mock<IRepository<Job>> _genericJobRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IAiScoreInvalidationService> _aiScoreInvalidationServiceMock = new();

    private User SetupActiveEmployerUser(Guid userId)
    {
        var user = new User { Id = userId, IsActive = true, UserRole = UserRole.Employer };
        _genericUserRepoMock.Setup(x => x.FirstOrDefaultAsync(It.IsAny<ISpecification<User>>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);
        return user;
    }

    private Employer SetupEmployerProfile(Guid userId, Guid employerId)
    {
        var employer = new Employer { Id = employerId, UserId = userId, CompanyName = "Tech Corp" };
        _genericEmployerRepoMock.Setup(x => x.FirstOrDefaultAsync(It.IsAny<ISpecification<Employer>>(), It.IsAny<CancellationToken>())).ReturnsAsync(employer);
        return employer;
    }

    [Fact]
    public async Task CreateJobHandler_ShouldThrowUnauthorized_WhenUserIsNotEmployer()
    {
        var userId = Guid.NewGuid();
        var handler = new CreateJobCommandHandler(
            _genericUserRepoMock.Object,
            _genericEmployerRepoMock.Object,
            _genericJobRepoMock.Object,
            _uowMock.Object,
            _aiScoreInvalidationServiceMock.Object,
            NullLogger<CreateJobCommandHandler>.Instance);

        var command = new CreateJobCommand(userId, false, "Dev", "", "", [], "FullTime", "Remote", "Junior", 0, 2, 50, 60, null, null, "UTC", "USD", 1, DateTimeOffset.UtcNow.AddDays(10));

        await Assert.ThrowsAsync<UnauthorizedException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task CreateJobHandler_ShouldCreateDraft_WhenValid()
    {
        var userId = Guid.NewGuid();
        var employerId = Guid.NewGuid();
        SetupActiveEmployerUser(userId);
        SetupEmployerProfile(userId, employerId);

        var handler = new CreateJobCommandHandler(
            _genericUserRepoMock.Object,
            _genericEmployerRepoMock.Object,
            _genericJobRepoMock.Object,
            _uowMock.Object,
            _aiScoreInvalidationServiceMock.Object,
            NullLogger<CreateJobCommandHandler>.Instance);

        var command = new CreateJobCommand(userId, false, "Software Engineer", "Desc", "Resp", ["Skills"], "FullTime", "Remote", "Junior", 0, 2, 50, 60, null, null, "UTC", "USD", 1, DateTimeOffset.UtcNow.AddDays(10));

        var response = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(JobStatus.Draft.ToString(), response.Status);
        _genericJobRepoMock.Verify(x => x.AddAsync(It.Is<Job>(job =>
            job.Skills.Count == 1
            && job.Skills.Single().Name == "Skills"
            && job.Skills.Single().DisplayOrder == 0), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ...existing code...
    [Fact]
    public async Task UpdateJobStatusHandler_ShouldUpdateStatus_WhenValid()
    {
        var userId = Guid.NewGuid();
        var employerId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        SetupActiveEmployerUser(userId);
        SetupEmployerProfile(userId, employerId);
        // Only jobs in Draft or Deactivated can be activated (set to Active)
        var job = new Job
        {
            Id = jobId,
            EmployerId = employerId,
            Status = JobStatus.Draft,
            ApplicationDeadline = DateTimeOffset.UtcNow.AddDays(5) // ensure not expired
        };
        _genericJobRepoMock.Setup(x => x.FirstOrDefaultAsync(It.IsAny<ISpecification<Job>>(), It.IsAny<CancellationToken>())).ReturnsAsync(job);

        var handler = new UpdateJobStatusCommandHandler(_genericUserRepoMock.Object, _genericEmployerRepoMock.Object, _genericJobRepoMock.Object, _uowMock.Object, NullLogger<UpdateJobStatusCommandHandler>.Instance);

        var command = new UpdateJobStatusCommand(userId, jobId, "Activate");
        await handler.Handle(command, CancellationToken.None);

        Assert.Equal(JobStatus.Active, job.Status);
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
    [Fact]
    public async Task DeleteJobHandler_ShouldDeleteJob_WhenValid()
    {
        var userId = Guid.NewGuid();
        var employerId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var user = SetupActiveEmployerUser(userId);
        user.Email = "employer@example.com";
        SetupEmployerProfile(userId, employerId);
        var job = new Job { Id = jobId, EmployerId = employerId, Status = JobStatus.Draft };
        _genericJobRepoMock.Setup(x => x.FirstOrDefaultAsync(It.IsAny<ISpecification<Job>>(), It.IsAny<CancellationToken>())).ReturnsAsync(job);

        var handler = new DeleteJobCommandHandler(_genericUserRepoMock.Object, _genericEmployerRepoMock.Object, _genericJobRepoMock.Object, _uowMock.Object, NullLogger<DeleteJobCommandHandler>.Instance);

        var command = new DeleteJobCommand(userId, jobId);
        await handler.Handle(command, CancellationToken.None);

        // The handler should set soft delete properties
        Assert.True(job.Deleted);
        Assert.NotNull(job.DeletedOn);
        Assert.Equal("employer@example.com", job.DeletedBy);

        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        // Should NOT call Remove
        _genericJobRepoMock.Verify(x => x.Remove(It.IsAny<Job>()), Times.Never);
    }

    // The above code block was a duplicate and/or used old repository mocks. All usages of _jobRepoMock, _userRepoMock, _employerRepoMock are now removed. All tests use only the generic repository mocks (_genericUserRepoMock, _genericEmployerRepoMock, _genericJobRepoMock).

    [Fact]
    public async Task GetJobByIdHandler_ShouldReturnJobDetails()
    {
        var userId = Guid.NewGuid();
        var employerId = Guid.NewGuid();
        var jobId = Guid.NewGuid();

        SetupActiveEmployerUser(userId);
        SetupEmployerProfile(userId, employerId);

        var job = new Job
        {
            Id = jobId,
            EmployerId = employerId,
            Title = "Senior Dev",
            EmploymentType = EmploymentType.FullTime,
            ApplicationDeadline = DateTimeOffset.UtcNow.AddDays(5),
            Skills =
            [
                new JobSkill { Name = "PostgreSQL", DisplayOrder = 1 },
                new JobSkill { Name = "C#", DisplayOrder = 0 }
            ]
        };
        _genericJobRepoMock.Setup(x => x.FirstOrDefaultAsync(It.IsAny<ISpecification<Job>>(), It.IsAny<CancellationToken>())).ReturnsAsync(job);

        var handler = new GetJobByIdQueryHandler(_genericUserRepoMock.Object, _genericEmployerRepoMock.Object, _genericJobRepoMock.Object, _uowMock.Object, NullLogger<GetJobByIdQueryHandler>.Instance);

        var response = await handler.Handle(new GetJobByIdQuery(userId, jobId), CancellationToken.None);

        Assert.Equal("Senior Dev", response.Title);
        Assert.Equal(jobId, response.JobId);
        Assert.Equal(["C#", "PostgreSQL"], response.Skills);
    }

    // Removed obsolete GetEmployerDashboardHandler_ShouldReturnPagedResults test that used old repo mocks.

    // Removed obsolete GetPublicJobsHandler_ShouldReturnJobs test that used old repo mocks.
}
