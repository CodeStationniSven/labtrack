using MediatR;

namespace LabTrack.Application.Commands.AiChat.Financials;

public sealed record CreateExpenseCommand : IRequest<Guid>
{
    public Guid LaboratoryId { get; init; }
    public Guid? WorkstationId { get; init; }
    public Guid? AssetId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "PHP";
    public DateTime ExpenseDateUtc { get; init; } = DateTime.UtcNow;
    public string? Description { get; init; }
    public string? VendorOrSupplier { get; init; }
    public string? InvoiceOrReceiptNumber { get; init; }
    public Guid CreatedByUserId { get; init; }

    public Guid RecordedByUserId
    {
        get => CreatedByUserId;
        init => CreatedByUserId = value;
    }
}
