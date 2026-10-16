using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public sealed class InvitationConfiguration : IEntityTypeConfiguration<Invitation>
{
    public void Configure(EntityTypeBuilder<Invitation> builder)
    {
        builder.ToTable("Invitations");

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

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasOne(x => x.Job)
            .WithMany(x => x.Invitations)
            .HasForeignKey(x => x.JobId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Talent)
            .WithMany(x => x.Invitations)
            .HasForeignKey(x => x.TalentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.JobId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.TalentId);
    }
}
