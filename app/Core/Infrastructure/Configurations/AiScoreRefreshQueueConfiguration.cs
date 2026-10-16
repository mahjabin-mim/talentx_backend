using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public sealed class AiScoreRefreshQueueConfiguration : IEntityTypeConfiguration<AiScoreRefreshQueue>
{
    public void Configure(EntityTypeBuilder<AiScoreRefreshQueue> builder)
    {
        builder.ToTable("AI_score_refresh_queue");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd()
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.JobId)
            .IsRequired()
            .HasColumnType("uuid");

        builder.Property(x => x.TalentId)
            .IsRequired()
            .HasColumnType("uuid");

        builder.Property(x => x.Reason)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.RequestedAt)
            .IsRequired();

        builder.Property(x => x.AttemptCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.LastError)
            .HasMaxLength(1000);

        builder.HasIndex(x => new { x.JobId, x.TalentId })
            .IsUnique();

        builder.HasIndex(x => x.RequestedAt);

        builder.HasOne<Job>()
            .WithMany()
            .HasForeignKey(x => x.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<TalentProfile>()
            .WithMany()
            .HasForeignKey(x => x.TalentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
