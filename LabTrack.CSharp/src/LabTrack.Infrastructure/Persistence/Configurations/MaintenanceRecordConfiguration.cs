using LabTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LabTrack.Infrastructure.Persistence.Configurations;

public class MaintenanceRecordConfiguration : IEntityTypeConfiguration<MaintenanceRecord>
{
    public void Configure(EntityTypeBuilder<MaintenanceRecord> builder)
    {
        builder.ToTable("MaintenanceRecords");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .ValueGeneratedNever();

        builder.Property(m => m.AssetId)
            .IsRequired();

        builder.Property(m => m.PerformedByUserId)
            .IsRequired();

        builder.Property(m => m.Description)
            .HasMaxLength(2000);

        builder.Property(m => m.MaintenanceType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(m => m.CreatedAtUtc)
            .IsRequired();

        builder.Property(m => m.LastModifiedAtUtc);

        builder.HasOne(m => m.Asset)
            .WithMany()
            .HasForeignKey(m => m.AssetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.PerformedByUser)
            .WithMany()
            .HasForeignKey(m => m.PerformedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => m.AssetId);
        builder.HasIndex(m => m.PerformedByUserId);
    }
}