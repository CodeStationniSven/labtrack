using LabTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LabTrack.Infrastructure.Persistence.Configurations;

public class CommunicationConfiguration : IEntityTypeConfiguration<Communication>
{
    public void Configure(EntityTypeBuilder<Communication> builder)
    {
        builder.ToTable("Communications");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .ValueGeneratedNever();

        builder.Property(c => c.SenderId)
            .IsRequired();

        builder.Property(c => c.RecipientId)
            .IsRequired();

        builder.Property(c => c.Subject)
            .HasMaxLength(200)
            .IsRequired();

        // Pinalitan ang nvarchar(max) ng text para sa PostgreSQL
        builder.Property(c => c.Body)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(c => c.Channel)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(c => c.RelatedEntityName)
            .HasMaxLength(100);

        builder.Property(c => c.RelatedEntityId)
            .HasMaxLength(100);

        builder.Property(c => c.SentAt)
            .IsRequired(false);

        builder.Property(c => c.ReadAt)
            .IsRequired(false);

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        // Foreign Key Relationships
        builder.HasOne(c => c.Sender)
            .WithMany()
            .HasForeignKey(c => c.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Recipient)
            .WithMany()
            .HasForeignKey(c => c.RecipientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.SenderId);
        builder.HasIndex(c => c.RecipientId);
        builder.HasIndex(c => c.CreatedAt);
        builder.HasIndex(c => new { c.RelatedEntityName, c.RelatedEntityId });
    }
}