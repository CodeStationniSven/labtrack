using LabTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LabTrack.Infrastructure.Persistence.Configurations;

public class DamageAttributionConfiguration : IEntityTypeConfiguration<DamageAttribution>
{
    public void Configure(EntityTypeBuilder<DamageAttribution> builder)
    {
        builder.ToTable("DamageAttributions");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .ValueGeneratedNever();

        builder.Property(d => d.DamageDescription)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(d => d.Severity)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(d => d.Status)
            .HasMaxLength(50)
            .IsRequired();

        // FK: ReportedByUser → ReportedByUserId
        builder.HasOne(d => d.ReportedByUser)
            .WithMany(u => u.ReportedDamages)
            .HasForeignKey(d => d.ReportedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // FK: ResponsibleUser → ResponsibleUserId (nullable)
        builder.HasOne(d => d.ResponsibleUser)
            .WithMany(u => u.ResponsibleDamages)
            .HasForeignKey(d => d.ResponsibleUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // FK: Asset → AssetId
        builder.HasOne(d => d.Asset)
            .WithMany()
            .HasForeignKey(d => d.AssetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => d.AssetId);
        builder.HasIndex(d => d.ReportedByUserId);
        builder.HasIndex(d => d.ResponsibleUserId);
        builder.HasIndex(d => d.Status);
    }
}