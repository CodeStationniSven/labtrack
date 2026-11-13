namespace LabTrack.Application.Dtos.Notifications;

public sealed record NotificationDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string Type { get; init; } = "Info"; // e.g., Info, Warning, Alert, System, Maintenance
    public string Severity { get; init; } = "Normal"; // e.g., Low, Normal, High, Urgent

    // Action / Routing Metadata
    public string? ActionUrl { get; init; }
    public string? TargetEntity { get; init; } // e.g., MaintenanceTicket, Incident, Expense
    public Guid? TargetEntityId { get; init; }

    // Read State & Timestamps
    public bool IsRead { get; init; }
    public DateTime? ReadAtUtc { get; init; }
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
    public DateTime? ExpiresAtUtc { get; init; }
}