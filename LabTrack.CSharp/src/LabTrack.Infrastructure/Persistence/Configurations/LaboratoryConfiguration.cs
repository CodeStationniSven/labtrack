using LabTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LabTrack.Infrastructure.Persistence.Configurations;

public class LaboratoryConfiguration : IEntityTypeConfiguration<Laboratory>
{
    public void Configure(EntityTypeBuilder<Laboratory> builder)
    {
        builder.ToTable("Laboratories");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id)
            .ValueGeneratedNever();

        builder.Property(l => l.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(l => l.Building)
            .HasMaxLength(100);

        builder.Property(l => l.RoomNumber)
            .HasMaxLength(50);

        builder.Property(l => l.Capacity);

        builder.Property(l => l.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(l => l.CreatedAtUtc)
            .IsRequired();

        builder.Property(l => l.LastModifiedAtUtc);

        builder.HasIndex(l => l.IsActive);

        builder.HasMany(l => l.Assets)
            .WithOne(a => a.Laboratory)
            .HasForeignKey(a => a.LaboratoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}