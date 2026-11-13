using LabTrack.Domain.Common;

namespace LabTrack.Domain.Entities;

public class ChainOfCustodyEvent : AuditableEntity
{
    public Guid AssetId { get; set; }
    public Guid ReleasedByUserId { get; set; }
    public Guid ReceivedByUserId { get; set; }
    public string TransferType { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime TransferredAtUtc { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }

    // Navigation properties
    public virtual Asset Asset { get; set; } = null!;
    public virtual User ReleasedByUser { get; set; } = null!;
    public virtual User ReceivedByUser { get; set; } = null!;
}