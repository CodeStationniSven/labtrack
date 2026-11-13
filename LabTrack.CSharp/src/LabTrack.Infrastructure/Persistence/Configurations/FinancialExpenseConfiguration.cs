using LabTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LabTrack.Infrastructure.Persistence.Configurations;

public class FinancialExpenseConfiguration : IEntityTypeConfiguration<FinancialExpense>
{
    public void Configure(EntityTypeBuilder<FinancialExpense> builder)
    {
        builder.ToTable("FinancialExpenses");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.Category)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(e => e.ExpenseDateUtc)
            .IsRequired();

        builder.Property(e => e.CreatedAtUtc)
            .IsRequired();

        builder.Property(e => e.LastModifiedAtUtc);

        builder.HasIndex(e => e.Category);
        builder.HasIndex(e => e.ExpenseDateUtc);
        builder.HasIndex(e => e.LaboratoryId);
        builder.HasIndex(e => e.AssetId);
    }
}