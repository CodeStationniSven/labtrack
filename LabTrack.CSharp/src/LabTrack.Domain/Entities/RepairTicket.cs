using LabTrack.Domain.Common;

namespace LabTrack.Domain.Entities;

public class RepairTicket : AuditableEntity
{
    public Guid AssetId { get; set; }
    public Guid ReportedByUserId { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty; // e.g., Low, Medium, High, Urgent
    public string Status { get; set; } = string.Empty; // e.g., Open, InProgress, OnHold, Resolved, Closed
    public DateTime ReportedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public decimal? Cost { get; set; }
    public string? ResolutionNotes { get; set; }

    // Navigation properties
    public virtual Asset Asset { get; set; } = null!;
    public virtual User ReportedByUser { get; set; } = null!;
    public virtual User? AssignedToUser { get; set; }
    public virtual ICollection<ReplacementRecord> ReplacementRecords { get; set; } = new List<ReplacementRecord>();
}