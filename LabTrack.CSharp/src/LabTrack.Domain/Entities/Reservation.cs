using LabTrack.Domain.Common;

namespace LabTrack.Domain.Entities;

public class Reservation : AuditableEntity
{
    public Guid UserId { get; set; }
    public Guid LaboratoryId { get; set; }
    public Guid? WorkstationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public DateTime StartTimeUtc { get; set; }
    public DateTime EndTimeUtc { get; set; }
    public string Status { get; set; } = string.Empty; // e.g., Pending, Confirmed, Cancelled, Completed
    public string? CancellationReason { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual Laboratory Laboratory { get; set; } = null!;
    public virtual Workstation? Workstation { get; set; }
}