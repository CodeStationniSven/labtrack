namespace LabTrack.Application.Dtos.Audit;

public sealed record AuditLogDto
{
    public Guid Id { get; init; }
    public Guid? UserId { get; init; }
    public string? UserName { get; init; }
    public string Action { get; init; } = string.Empty; // e.g., Create, Update, Delete, Login, Scan
    public string EntityName { get; init; } = string.Empty; // e.g., Asset, Workstation, Reservation
    public string? EntityId { get; init; }
    
    // Change Details
    public string? OldValues { get; init; } // JSON string of previous state
    public string? NewValues { get; init; } // JSON string of new state
    public string? AffectedColumns { get; init; }

    // Client Context
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}