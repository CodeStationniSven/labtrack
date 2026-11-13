namespace LabTrack.Domain.Common;

/// <summary>
/// Base class for all entities that support soft delete (archive).
/// </summary>
public abstract class AuditableEntity
{
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastModifiedAtUtc { get; set; }
}