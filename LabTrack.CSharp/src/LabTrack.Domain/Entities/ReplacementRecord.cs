namespace LabTrack.Domain.Entities;

public class ReplacementRecord : Common.BaseEntity
{
    public Guid LaboratoryId { get; set; }
    public Guid? WorkstationId { get; set; }
    public Guid? OriginalAssetId { get; set; }
    public Guid? ReplacementAssetId { get; set; }

    public string ItemName { get; set; } = string.Empty;
    public string Category { get; set; } = "Peripheral";
    public int Quantity { get; set; } = 1;
    public string Urgency { get; set; } = "Medium";
    public string Status { get; set; } = "Pending";
    public string Justification { get; set; } = string.Empty;

    public decimal? EstimatedCost { get; set; }
    public decimal? ApprovedCost { get; set; }
    public string? PreferredVendor { get; set; }

    public string? DecisionNotes { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }

    public Guid RequestedByUserId { get; set; }
    public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? FulfilledAtUtc { get; set; }

    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
}