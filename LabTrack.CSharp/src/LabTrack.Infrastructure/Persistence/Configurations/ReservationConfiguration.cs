using LabTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LabTrack.Infrastructure.Persistence.Configurations;

public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("Reservations");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .ValueGeneratedNever();

        builder.Property(r => r.Purpose)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(r => r.Status)
            .HasMaxLength(30)
            .IsRequired();

        builder.HasOne(r => r.Laboratory)
            .WithMany()
            .HasForeignKey(r => r.LaboratoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.LaboratoryId);
        builder.HasIndex(r => r.Status);
    }
}