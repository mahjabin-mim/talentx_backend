using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public sealed class TalentProfileSkillConfiguration : IEntityTypeConfiguration<TalentProfileSkill>
{
    public void Configure(EntityTypeBuilder<TalentProfileSkill> builder)
    {
        builder.ToTable("TalentProfileSkills");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd()
            .HasColumnType("uuid");

        builder.Property(x => x.TalentProfileId)
            .IsRequired()
            .HasColumnType("uuid");

        builder.HasOne(x => x.TalentProfile)
            .WithMany(x => x.Skills)
            .HasForeignKey(x => x.TalentProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.Language)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.YearsOfExperience)
            .IsRequired()
            .HasPrecision(4, 1);

        builder.Property(x => x.DisplayOrder)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasIndex(x => new { x.TalentProfileId, x.Language })
            .IsUnique();

        builder.HasIndex(x => new { x.TalentProfileId, x.DisplayOrder })
            .IsUnique();
    }
}
