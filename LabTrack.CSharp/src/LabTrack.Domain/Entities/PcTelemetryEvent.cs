using LabTrack.Domain.Common;

namespace LabTrack.Domain.Entities;

public class PcTelemetryEvent : BaseEntity
{
    public Guid WorkstationId { get; set; }
    public double CpuUsagePercent { get; set; }
    public double MemoryUsagePercent { get; set; }
    public double DiskUsagePercent { get; set; }
    public double? TemperatureCelsius { get; set; }
    public string SystemStatus { get; set; } = string.Empty; // e.g., Healthy, Warning, Critical
    public DateTime LoggedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation property
    public virtual Workstation Workstation { get; set; } = null!;

    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
}