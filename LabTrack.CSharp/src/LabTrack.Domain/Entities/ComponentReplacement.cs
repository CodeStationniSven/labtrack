namespace LabTrack.Domain.Entities;

public class ComponentReplacement
{
    public Guid Id { get; set; }

    public Guid AssetId { get; set; }
   

    public Guid? OldComponentId { get; set; }
   

    public Guid? NewComponentId { get; set; }
    

    public string ComponentType { get; set; } = string.Empty;
   

    public string OldComponentDetails { get; set; } = string.Empty;
    

    public string NewComponentDetails { get; set; } = string.Empty;
    

    public string Reason { get; set; } = string.Empty;
  

    public Guid ReplacedByUserId { get; set; }

    public DateTime ReplacedAtUtc { get; set; } = DateTime.UtcNow;

    public Guid? RelatedRepairTicketId { get; set; }
   

    public decimal? Cost { get; set; }

    public string Status { get; set; } = "COMPLETED";
   

    public string? Notes { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
}