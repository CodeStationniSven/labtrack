using LabTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LabTrack.Infrastructure.Persistence.Configurations;

public class ProblemReportConfiguration : IEntityTypeConfiguration<ProblemReport>
{
    public void Configure(EntityTypeBuilder<ProblemReport> builder)
    {
        builder.ToTable("ProblemReports");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .ValueGeneratedNever();

        builder.Property(r => r.ReportedByUserId)
            .IsRequired();

        builder.Property(r => r.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(r => r.Description)
            .HasMaxLength(2000);

        builder.Property(r => r.Category)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.Priority)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(r => r.Status)
            .HasMaxLength(30)
            .IsRequired();

        builder.HasOne(r => r.ReportedByUser)
            .WithMany()
            .HasForeignKey(r => r.ReportedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Asset)
            .WithMany()
            .HasForeignKey(r => r.AssetId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(r => r.Laboratory)
            .WithMany()
            .HasForeignKey(r => r.LaboratoryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(r => r.ReportedByUserId);
        builder.HasIndex(r => r.AssetId);
        builder.HasIndex(r => r.LaboratoryId);
        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => r.Priority);
    }
}