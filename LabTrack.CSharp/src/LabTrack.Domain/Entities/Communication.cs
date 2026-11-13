using LabTrack.Domain.Common;

namespace LabTrack.Domain.Entities;

public class Communication : BaseEntity
{
    public Guid SenderId { get; set; }
    public Guid RecipientId { get; set; }

    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;

    public string Channel { get; set; } = "InApp";
    // e.g., InApp, Email, Sms, Push

    public string Status { get; set; } = "Pending";
    // e.g., Pending, Sent, Delivered, Read, Failed

    public string? RelatedEntityName { get; set; }
    public string? RelatedEntityId { get; set; }

    public DateTime? SentAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual User Sender { get; set; } = null!;
    public virtual User Recipient { get; set; } = null!;

    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
}