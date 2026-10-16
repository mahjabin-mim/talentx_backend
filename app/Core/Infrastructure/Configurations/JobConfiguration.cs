using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public sealed class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.ToTable("Jobs");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.EmployerId)
            .IsRequired()
            .HasColumnType("uuid");

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(160);

        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(x => x.Responsibilities)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(x => x.EmploymentType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.WorkArrangement)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.ExperienceLevel)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(JobStatus.Draft)
            .HasSentinel((JobStatus)0);

        builder.Property(x => x.ExperienceMinYears)
            .IsRequired();

        builder.Property(x => x.ExperienceMaxYears)
            .IsRequired();

        builder.Property(x => x.SalaryMin)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(x => x.SalaryMax)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(x => x.CountryCode)
            .HasMaxLength(3);

        builder.Property(x => x.City)
            .HasMaxLength(120);

        builder.Property(x => x.Timezone)
            .HasMaxLength(64);

        builder.Property(x => x.CurrencyCode)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(x => x.NumberOfPositions)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(x => x.ApplicationDeadline)
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(100);

        builder.Property(x => x.CreatedOn)
            .IsRequired();

        builder.Property(x => x.LastModifiedBy)
            .HasMaxLength(100);

        builder.Property(x => x.DeletedBy)
            .HasMaxLength(100);

        builder.Property(x => x.Deleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasIndex(x => x.EmployerId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.ApplicationDeadline);

        builder.HasOne(x => x.Employer)
            .WithMany(x => x.Jobs)
            .HasForeignKey(x => x.EmployerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}