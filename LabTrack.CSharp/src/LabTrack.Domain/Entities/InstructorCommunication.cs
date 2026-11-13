using LabTrack.Domain.Common;

namespace LabTrack.Domain.Entities;

public class InstructorCommunication : AuditableEntity
{
    public Guid InstructorUserId { get; set; }
    public Guid LaboratoryId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }

    // Navigation properties
    public virtual User InstructorUser { get; set; } = null!;
    public virtual Laboratory Laboratory { get; set; } = null!;
}