using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd()
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.Fullname)
            .HasMaxLength(120);

        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(254);

        builder.HasIndex(x => x.Email)
            .IsUnique();

        builder.Property(x => x.Password)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.ProfileImageURL)
            .HasMaxLength(500);

        builder.Property(x => x.PhoneNumber)
            .HasMaxLength(20);

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.EmailConfirmed)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.EmailVerificationCode)
            .HasMaxLength(128);

        builder.Property(x => x.ForgetPasswordVerificationCode)
            .HasMaxLength(128);

        builder.Property(x => x.RefreshToken)
            .HasMaxLength(128);

        builder.Property(x => x.PreviousRefreshToken)
            .HasMaxLength(128);

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

        builder.Property(x => x.UserRole)
            .HasConversion<int?>();

        builder.Property(x => x.TokenVersion)
            .IsRequired()
            .HasDefaultValue(0);
    }
}
