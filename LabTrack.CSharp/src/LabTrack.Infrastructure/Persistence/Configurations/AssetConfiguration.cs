using LabTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LabTrack.Infrastructure.Persistence.Configurations;

public class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.ToTable("Assets");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .ValueGeneratedNever();

        builder.Property(a => a.AssetTag)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(a => a.AssetTag)
            .IsUnique();

        builder.Property(a => a.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(a => a.Category)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.SerialNumber)
            .HasMaxLength(100);

        builder.Property(a => a.Status)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(a => a.CreatedAtUtc)
            .IsRequired();

        builder.Property(a => a.LastModifiedAtUtc);

        builder.HasIndex(a => a.Status);
    }
}