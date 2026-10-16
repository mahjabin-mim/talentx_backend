using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public sealed class AiScoreConfiguration : IEntityTypeConfiguration<AiScore>
{
    public void Configure(EntityTypeBuilder<AiScore> builder)
    {
        builder.ToTable("AI_score");

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

        builder.Property(x => x.Score)
            .IsRequired()
            .HasPrecision(5, 2)
            .HasColumnName("AiScore");

        builder.Property(x => x.CalculatedAt)
            .IsRequired();

        builder.HasIndex(x => new { x.JobId, x.TalentId })
            .IsUnique();

        builder.HasOne(x => x.Job)
            .WithMany()
            .HasForeignKey(x => x.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Talent)
            .WithMany()
            .HasForeignKey(x => x.TalentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
