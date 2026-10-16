using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public sealed class EmployerConfiguration : IEntityTypeConfiguration<Employer>
{
    public void Configure(EntityTypeBuilder<Employer> builder)
    {
        builder.ToTable("Employers");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.UserId)
            .IsRequired()
            .HasColumnType("uuid");

        builder.HasIndex(x => x.UserId)
            .IsUnique();

        builder.Property(x => x.CompanyName)
            .IsRequired()
            .HasMaxLength(160);

        builder.HasOne(x => x.User)
            .WithOne(x => x.Employer)
            .HasForeignKey<Employer>(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
