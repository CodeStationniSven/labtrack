namespace LabTrack.Application.Dtos.Financials;

public sealed record UpdateFinancialExpenseDto
{
    public Guid? WorkstationId { get; init; }
    public Guid? AssetId { get; init; }

    public string? Title { get; init; }
    public string? Category { get; init; }
    public decimal? Amount { get; init; }
    public string? Currency { get; init; }

    public DateTime? ExpenseDateUtc { get; init; }
    public string? Description { get; init; }
    public string? VendorOrSupplier { get; init; }
    public string? InvoiceOrReceiptNumber { get; init; }
}