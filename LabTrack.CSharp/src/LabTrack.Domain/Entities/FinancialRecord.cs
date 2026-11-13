namespace LabTrack.Domain.Entities;

public class FinancialRecord
{
    public Guid Id { get; set; }
    public Guid AssetId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string RecordType { get; set; } = string.Empty;   // e.g., "PURCHASE", "REPAIR", "REPLACEMENT"
    public DateTime RecordedAtUtc { get; set; }
    public Guid RecordedByUserId { get; set; }
    public string? Notes { get; set; }

    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
}