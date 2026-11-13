using LabTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LabTrack.Infrastructure.Persistence.Configurations;

public class ReplacementRequestConfiguration : IEntityTypeConfiguration<ReplacementRequest>
{
    public void Configure(EntityTypeBuilder<ReplacementRequest> builder)
    {
        builder.ToTable("ReplacementRequests");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .ValueGeneratedNever();

        builder.Property(r => r.AssetId)
            .IsRequired();

        builder.Property(r => r.RequestedByUserId)
            .IsRequired();

        builder.Property(r => r.Reason)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(r => r.Status)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(r => r.ReviewNotes)
            .HasMaxLength(1000);

        builder.HasOne(r => r.Asset)
            .WithMany()
            .HasForeignKey(r => r.AssetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.RequestedByUser)
            .WithMany()
            .HasForeignKey(r => r.RequestedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.AssetId);
        builder.HasIndex(r => r.RequestedByUserId);
        builder.HasIndex(r => r.Status);
    }
}