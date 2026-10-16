using Domain.Entities;
using Domain.Enums;
using Modules.TalentProfiles.GetCurrent;
using UnitTests.Auth;
using Xunit;

namespace UnitTests.TalentProfiles;

public sealed class GetCurrentTalentProfileQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnEmptyProfile_WhenTalentProfileDoesNotExist()
    {
        await using var dbContext = TestUtilities.CreateDbContext();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Fullname = "New Talent",
            Email = "talent@example.com",
            Password = "hashed",
            IsActive = true,
            UserRole = UserRole.Talent,
            CreatedOn = DateTimeOffset.UtcNow
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var handler = new GetCurrentTalentProfileQueryHandler(dbContext);

        var result = await handler.Handle(
            new GetCurrentTalentProfileQuery(user.Id),
            CancellationToken.None);

        Assert.Equal("New Talent", result.Name);
        Assert.Equal("talent@example.com", result.Email);
        Assert.Equal(string.Empty, result.ResumeUrl);
        Assert.Equal(string.Empty, result.PictureUrl);
        Assert.Empty(result.Skills);
    }
}
