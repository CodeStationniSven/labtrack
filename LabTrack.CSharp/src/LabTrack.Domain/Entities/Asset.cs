using LabTrack.Domain.Common;
using LabTrack.Domain.Enums;

namespace LabTrack.Domain.Entities;

public class Asset : AuditableEntity
{
    public string AssetTag { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid LaboratoryId { get; set; }

    // Navigation properties
    public virtual Laboratory Laboratory { get; set; } = null!;
    public virtual ICollection<MaintenanceRecord> MaintenanceRecords { get; set; } = new List<MaintenanceRecord>();
    public virtual ICollection<RepairTicket> RepairTickets { get; set; } = new List<RepairTicket>();
    public virtual ICollection<ReplacementRecord> ReplacementRecords { get; set; } = new List<ReplacementRecord>();
}