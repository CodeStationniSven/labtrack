using LabTrack.Domain.Common;

namespace LabTrack.Domain.Entities;

public class NetworkIncident : AuditableEntity
{
    public Guid NetworkDeviceId { get; set; }
    public Guid? ReportedByUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty; // e.g., Low, Medium, High, Critical
    public string Status { get; set; } = string.Empty; // e.g., Open, Investigating, Resolved, Closed
    public DateTime IncidentDateUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAtUtc { get; set; }
    public string? ResolutionNotes { get; set; }

    // Navigation properties
    public virtual NetworkDevice NetworkDevice { get; set; } = null!;
    public virtual User? ReportedByUser { get; set; }
}