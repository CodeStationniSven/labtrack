using LabTrack.Infrastructure.Idempotency;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LabTrack.Infrastructure.Persistence.Configurations;

public class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("IdempotencyRecords");

        builder.HasKey(r => r.Key);

        builder.Property(r => r.Key)
            .HasMaxLength(256)
            .IsRequired();

        // Pinalitan ang nvarchar(max) ng text para sa PostgreSQL
        builder.Property(r => r.ResponseJson)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(r => r.CreatedAt)
            .IsRequired();

        builder.HasIndex(r => r.CreatedAt);
    }
}