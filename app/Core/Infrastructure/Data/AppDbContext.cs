using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AiScore> AiScores => Set<AiScore>();
    public DbSet<AiScoreRefreshQueue> AiScoreRefreshQueues => Set<AiScoreRefreshQueue>();
    public DbSet<Application> Applications => Set<Application>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<TalentProfile> TalentProfiles => Set<TalentProfile>();
    public DbSet<TalentProfileSkill> TalentProfileSkills => Set<TalentProfileSkill>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Employer> Employers => Set<Employer>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<JobSkill> JobSkills => Set<JobSkill>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        modelBuilder.Entity<Job>().HasQueryFilter(x => !x.Deleted);
        modelBuilder.Entity<User>().HasQueryFilter(x => !x.Deleted);
        modelBuilder.Entity<AiScore>().HasQueryFilter(x => !x.Job.Deleted);
        modelBuilder.Entity<Application>().HasQueryFilter(x => !x.Job.Deleted);
        modelBuilder.Entity<Invitation>().HasQueryFilter(x => !x.Job.Deleted);
        modelBuilder.Entity<JobSkill>().HasQueryFilter(x => !x.Job.Deleted);
        modelBuilder.Entity<Employer>().HasQueryFilter(x => !x.User.Deleted);
        modelBuilder.Entity<TalentProfile>().HasQueryFilter(x => !x.User.Deleted);
        modelBuilder.Entity<TalentProfileSkill>().HasQueryFilter(x => !x.TalentProfile.User.Deleted);
        base.OnModelCreating(modelBuilder);
    }
}
