using Modules.Jobs.CreateJob;
using Modules.Jobs.DeleteJob;
using Modules.Jobs.GetEmployerDashboard;
using Modules.Jobs.GetJobById;
using Modules.Jobs.UpdateJob;
using Modules.Jobs.UpdateJobStatus;
using Xunit;

namespace UnitTests.Jobs;

public class JobValidatorsTests
{
    [Fact]
    public void CreateJobValidator_ShouldFail_WhenTitleIsMissing()
    {
        var validator = new CreateJobCommandValidator();
        var command = new CreateJobCommand(
            Guid.NewGuid(), false, "", "", "", [], "FullTime", "Remote", "Junior",
            0, 2, 50000, 60000, null, null, "UTC", "USD", 1, DateTimeOffset.UtcNow.AddDays(10));

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "Title");
    }

    [Fact]
    public void CreateJobValidator_ShouldFail_WhenDeadlineIsInPast()
    {
        var validator = new CreateJobCommandValidator();
        var command = new CreateJobCommand(
            Guid.NewGuid(), false, "Software Engineer", "", "", [], "FullTime", "Remote", "Junior",
            0, 2, 50000, 60000, null, null, "UTC", "USD", 1, DateTimeOffset.UtcNow.AddDays(-1));

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "ApplicationDeadline");
    }

    [Fact]
    public void CreateJobValidator_ShouldFail_ForInvalidEnums()
    {
        var validator = new CreateJobCommandValidator();
        var command = new CreateJobCommand(
            Guid.NewGuid(), false, "Dev", "", "", [], "InvalidType", "Remote", "Junior",
            0, 2, 50000, 60000, null, null, "UTC", "USD", 1, DateTimeOffset.UtcNow.AddDays(10));

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "EmploymentType");
    }

    [Fact]
    public void UpdateJobValidator_ShouldFail_ForEmptyJobId()
    {
        var validator = new UpdateJobCommandValidator();
        var command = new UpdateJobCommand(
            Guid.NewGuid(), Guid.Empty, false, "Software Engineer", "", "", [], "FullTime", "Remote", "Junior",
            0, 2, 50000, 60000, null, null, "UTC", "USD", 1, DateTimeOffset.UtcNow.AddDays(10));

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "JobId");
    }

    [Fact]
    public void UpdateJobStatusValidator_ShouldFail_ForInvalidAction()
    {
        var validator = new UpdateJobStatusCommandValidator();
        var result = validator.Validate(new UpdateJobStatusCommand(Guid.NewGuid(), Guid.NewGuid(), "Delete"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "Action");
    }

    [Fact]
    public void DeleteJobValidator_ShouldFail_ForEmptyGuids()
    {
        var validator = new DeleteJobCommandValidator();
        var result = validator.Validate(new DeleteJobCommand(Guid.Empty, Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public void GetJobByIdValidator_ShouldFail_ForEmptyGuids()
    {
        var validator = new GetJobByIdQueryValidator();
        var result = validator.Validate(new GetJobByIdQuery(Guid.Empty, Guid.Empty));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void GetEmployerDashboardValidator_ShouldFail_ForEmptyUserId()
    {
        var validator = new GetEmployerDashboardQueryValidator();
        var result = validator.Validate(new GetEmployerDashboardQuery(Guid.Empty));

        Assert.False(result.IsValid);
    }
}
