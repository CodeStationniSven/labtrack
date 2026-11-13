using LabTrack.Domain.Common;

namespace LabTrack.Domain.Entities;

public class PcUsageLog : BaseEntity
{
    public Guid WorkstationId { get; set; }
    public Guid? UserId { get; set; }
    public DateTime SessionStartUtc { get; set; } = DateTime.UtcNow;
    public DateTime? SessionEndUtc { get; set; }
    public double? DurationMinutes { get; set; }
    public string Status { get; set; } = string.Empty; // e.g., Active, Completed, Terminated

    // Navigation properties
    public virtual Workstation Workstation { get; set; } = null!;
    public virtual User? User { get; set; }

    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
}