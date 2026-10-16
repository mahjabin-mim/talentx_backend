using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public sealed class TalentProfileConfiguration : IEntityTypeConfiguration<TalentProfile>
{
    public void Configure(EntityTypeBuilder<TalentProfile> builder)
    {
        builder.ToTable("TalentProfiles");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd()
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.UserId)
            .IsRequired()
            .HasColumnType("uuid");

        builder.HasIndex(x => x.UserId)
            .IsUnique();

        builder.HasOne(x => x.User)
            .WithOne()
            .HasForeignKey<TalentProfile>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.ResumeUrl)
            .HasMaxLength(500);

        builder.Property(x => x.ResumeFileName)
            .HasMaxLength(255);

        builder.Property(x => x.PictureUrl)
            .HasMaxLength(500);

        builder.Property(x => x.PictureFileName)
            .HasMaxLength(255);

        builder.Property(x => x.Location)
            .HasMaxLength(100);

        builder.Property(x => x.Tagline)
            .HasMaxLength(150);

        builder.Property(x => x.Bio)
            .HasMaxLength(2000);

        builder.Property(x => x.Linkedin)
            .HasMaxLength(500);

        builder.Property(x => x.AdditionalUrl)
            .HasMaxLength(500);

        builder.Property(x => x.VideoIntro)
            .HasMaxLength(500);

        builder.Property(x => x.Experience)
            .IsRequired();

        builder.Property(x => x.Workplaces)
            .HasMaxLength(2000);

        builder.Property(x => x.ExpectedSalary)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(x => x.CreatedAt)
            .IsRequired();
    }
}
