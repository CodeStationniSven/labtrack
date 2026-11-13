using LabTrack.Domain.Common;

namespace LabTrack.Domain.Entities;

public class MaintenanceSchedule : AuditableEntity
{
    public Guid AssetId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Frequency { get; set; } = string.Empty; // e.g., Daily, Weekly, Monthly, Annually
    public DateTime ScheduledDateUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string Status { get; set; } = string.Empty; // e.g., Pending, Completed, Overdue, Cancelled
    public Guid? AssignedToUserId { get; set; }

    // Navigation properties
    public virtual Asset Asset { get; set; } = null!;
    public virtual User? AssignedToUser { get; set; }
}