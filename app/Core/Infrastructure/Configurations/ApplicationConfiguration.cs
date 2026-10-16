using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public sealed class ApplicationConfiguration : IEntityTypeConfiguration<Application>
{
    public void Configure(EntityTypeBuilder<Application> builder)
    {
        builder.ToTable("Applications");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd()
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.JobId)
            .IsRequired();

        builder.HasOne(x => x.Job)
            .WithMany(x => x.Applications)
            .HasForeignKey(x => x.JobId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.TalentId)
            .IsRequired();

        builder.HasOne(x => x.Talent)
            .WithMany(x => x.Applications)
            .HasForeignKey(x => x.TalentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.Name)
            .HasMaxLength(100);

        builder.Property(x => x.ExpectedSalary)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(x => x.Email)
            .HasMaxLength(255);

        builder.Property(x => x.Experience)
            .IsRequired();

        builder.Property(x => x.Location)
            .HasMaxLength(100);

        builder.Property(x => x.ResumeUrl)
            .HasColumnName("ResumeId")
            .HasMaxLength(255);

        builder.Property(x => x.LinkedInUrl)
            .HasMaxLength(255);

        builder.Property(x => x.AdditionalUrl)
            .HasMaxLength(255);

        builder.Property(x => x.ContactNumber)
            .HasMaxLength(20);

        builder.Property(x => x.Education)
            .HasColumnType("text");

        builder.Property(x => x.Department)
            .HasMaxLength(255);

        builder.Property(x => x.CGPA)
            .HasMaxLength(50);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.CreatedAt)
            .IsRequired();
    }
}
