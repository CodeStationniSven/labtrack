using LabTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LabTrack.Infrastructure.Persistence.Configurations;

public class NetworkIncidentConfiguration : IEntityTypeConfiguration<NetworkIncident>
{
    public void Configure(EntityTypeBuilder<NetworkIncident> builder)
    {
        builder.ToTable("NetworkIncidents");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .ValueGeneratedNever();

        builder.Property(i => i.ReportedByUserId)
            .IsRequired();

        builder.Property(i => i.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(i => i.Description)
            .HasMaxLength(2000);

        builder.Property(i => i.Severity)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(i => i.Status)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(i => i.ResolutionNotes)
            .HasMaxLength(2000);

        builder.Property(i => i.CreatedAtUtc)
            .IsRequired();

        builder.Property(i => i.LastModifiedAtUtc);

        builder.HasOne(i => i.ReportedByUser)
            .WithMany()
            .HasForeignKey(i => i.ReportedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => i.ReportedByUserId);
        builder.HasIndex(i => i.Severity);
        builder.HasIndex(i => i.Status);
    }
}