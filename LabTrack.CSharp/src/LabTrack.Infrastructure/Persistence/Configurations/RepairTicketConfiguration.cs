using LabTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LabTrack.Infrastructure.Persistence.Configurations;

public class RepairTicketConfiguration : IEntityTypeConfiguration<RepairTicket>
{
    public void Configure(EntityTypeBuilder<RepairTicket> builder)
    {
        builder.ToTable("RepairTickets");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .ValueGeneratedNever();

        builder.Property(t => t.AssetId)
            .IsRequired();

        builder.Property(t => t.ReportedByUserId)
            .IsRequired();

        builder.Property(t => t.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.Description)
            .HasMaxLength(2000);

        builder.Property(t => t.Status)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(t => t.Priority)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(t => t.ResolutionNotes)
            .HasMaxLength(2000);

        builder.HasOne(t => t.Asset)
            .WithMany()
            .HasForeignKey(t => t.AssetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.ReportedByUser)
            .WithMany()
            .HasForeignKey(t => t.ReportedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => t.AssetId);
        builder.HasIndex(t => t.ReportedByUserId);
        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.Priority);
    }
}