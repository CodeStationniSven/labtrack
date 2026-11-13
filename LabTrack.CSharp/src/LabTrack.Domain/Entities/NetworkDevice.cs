using LabTrack.Domain.Common;

namespace LabTrack.Domain.Entities;

public class NetworkDevice : AuditableEntity
{
    public Guid AssetId { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty; // e.g., Switch, Router, Access Point, Server
    public string? FirmwareVersion { get; set; }
    public string Status { get; set; } = string.Empty; // e.g., Online, Offline, Maintenance
    public DateTime? LastPingUtc { get; set; }

    // Navigation property
    public virtual Asset Asset { get; set; } = null!;
}