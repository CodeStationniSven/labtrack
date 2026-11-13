namespace LabTrack.Application.Dtos.Financials;

public sealed record FinancialExpenseDto
{
    public Guid Id { get; init; }
    public Guid LaboratoryId { get; init; }
    public string LaboratoryName { get; init; } = string.Empty;
    public Guid? WorkstationId { get; init; }
    public string? WorkstationName { get; init; }
    public Guid? AssetId { get; init; }
    public string? AssetName { get; init; }

    public string Title { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty; // e.g., Maintenance, HardwareUpgrade, Utility, SoftwareLicense, Consumables
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "PHP";

    public DateTime ExpenseDateUtc { get; init; }
    public string? Description { get; init; }
    public string? VendorOrSupplier { get; init; }
    public string? InvoiceOrReceiptNumber { get; init; }

    // Audit Info
    public Guid CreatedByUserId { get; init; }
    public string CreatedByUserName { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
}