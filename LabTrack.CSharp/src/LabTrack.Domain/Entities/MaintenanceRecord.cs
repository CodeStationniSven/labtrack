using LabTrack.Domain.Common;

namespace LabTrack.Domain.Entities;

public class MaintenanceRecord : AuditableEntity
{
    public Guid AssetId { get; set; }
    public Guid PerformedByUserId { get; set; }
    public string MaintenanceType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime PerformedAtUtc { get; set; }
    public DateTime? NextScheduledUtc { get; set; }
    public decimal? Cost { get; set; }
    public string Status { get; set; } = string.Empty;

    // Navigation properties
    public virtual Asset Asset { get; set; } = null!;
    public virtual User PerformedByUser { get; set; } = null!;
}