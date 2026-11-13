using LabTrack.Domain.Common;

namespace LabTrack.Domain.Entities;

public class QrCode : BaseEntity
{
    public Guid EntityId { get; set; }
    public string EntityType { get; set; } = string.Empty; // e.g., Asset, Workstation, Laboratory
    public string CodeValue { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAtUtc { get; set; }
    public bool IsActive { get; set; } = true;

    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
}