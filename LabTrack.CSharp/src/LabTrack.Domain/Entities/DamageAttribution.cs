using LabTrack.Domain.Common;

namespace LabTrack.Domain.Entities;

public class DamageAttribution : AuditableEntity
{
    public Guid AssetId { get; set; }
    public Guid ReportedByUserId { get; set; }
    public Guid? ResponsibleUserId { get; set; }
    public string DamageDescription { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public decimal? EstimatedRepairCost { get; set; }
    public DateTime IncidentDateUtc { get; set; }
    public string Status { get; set; } = string.Empty;

    // Navigation properties
    public virtual Asset Asset { get; set; } = null!;
    public virtual User ReportedByUser { get; set; } = null!;
    public virtual User? ResponsibleUser { get; set; }
}