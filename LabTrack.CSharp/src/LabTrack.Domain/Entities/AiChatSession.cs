using LabTrack.Domain.Common;

namespace LabTrack.Domain.Entities;

public class AiChatSession : AuditableEntity
{
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAtUtc { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual ICollection<AiChatMessage> Messages { get; set; } = new List<AiChatMessage>();
}