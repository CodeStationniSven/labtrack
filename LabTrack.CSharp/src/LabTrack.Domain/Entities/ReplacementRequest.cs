using LabTrack.Domain.Common;

namespace LabTrack.Domain.Entities;

public class ReplacementRequest : AuditableEntity
{
    public Guid AssetId { get; set; }
    public Guid RequestedByUserId { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public Guid? RepairTicketId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty; // e.g., Low, Medium, High, Critical
    public string Status { get; set; } = string.Empty; // e.g., Pending, Approved, Rejected, Fulfilled, Cancelled
    public decimal? EstimatedCost { get; set; }
    public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAtUtc { get; set; }
    public string? ReviewNotes { get; set; }

    // Navigation properties
    public virtual Asset Asset { get; set; } = null!;
    public virtual User RequestedByUser { get; set; } = null!;
    public virtual User? ApprovedByUser { get; set; }
    public virtual RepairTicket? RepairTicket { get; set; }
}