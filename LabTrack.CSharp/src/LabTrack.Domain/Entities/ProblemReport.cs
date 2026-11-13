using LabTrack.Domain.Common;

namespace LabTrack.Domain.Entities;

public class ProblemReport : AuditableEntity
{
    public Guid ReportedByUserId { get; set; }
    public Guid? AssetId { get; set; }
    public Guid? WorkstationId { get; set; }
    public Guid? LaboratoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // e.g., Hardware, Software, Facility, Network
    public string Priority { get; set; } = string.Empty; // e.g., Low, Medium, High, Urgent
    public string Status { get; set; } = string.Empty; // e.g., Submitted, UnderReview, InProgress, Resolved, Rejected
    public DateTime ReportedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAtUtc { get; set; }
    public string? ResolutionNotes { get; set; }

    // Navigation properties
    public virtual User ReportedByUser { get; set; } = null!;
    public virtual Asset? Asset { get; set; }
    public virtual Workstation? Workstation { get; set; }
    public virtual Laboratory? Laboratory { get; set; }
}