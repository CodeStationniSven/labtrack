using LabTrack.Domain.Common;

namespace LabTrack.Domain.Entities;

public class FinancialExpense : AuditableEntity
{
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Description { get; set; } = string.Empty;
    public DateTime ExpenseDateUtc { get; set; }
    public Guid RecordedByUserId { get; set; }
    public Guid? LaboratoryId { get; set; }
    public Guid? AssetId { get; set; }

    // Navigation properties
    public virtual User RecordedByUser { get; set; } = null!;
    public virtual Laboratory? Laboratory { get; set; }
    public virtual Asset? Asset { get; set; }
}