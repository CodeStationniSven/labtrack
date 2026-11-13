using LabTrack.Domain.Common;

namespace LabTrack.Domain.Entities;

public class AiChatMessage : AuditableEntity
{
    public Guid UserId { get; set; }
    public Guid? SessionId { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public string? ModelUsed { get; set; }
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public double ResponseTimeMs { get; set; }

    // Navigation property
    public virtual User User { get; set; } = null!;
}