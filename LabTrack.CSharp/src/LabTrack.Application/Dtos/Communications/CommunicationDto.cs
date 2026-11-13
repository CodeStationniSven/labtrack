namespace LabTrack.Application.Dtos.Communications;

public sealed record CommunicationDto
{
    public Guid Id { get; init; }
    public string Subject { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string Priority { get; init; } = "Normal"; // e.g., Low, Normal, High, Urgent
    public string Type { get; init; } = "Announcement"; // e.g., Announcement, Alert, MaintenanceNotice, DirectMessage
    public bool IsBroadcast { get; init; }

    // Target Scope
    public Guid? TargetLaboratoryId { get; init; }
    public string? TargetLaboratoryName { get; init; }
    public string? TargetRole { get; init; } // e.g., All, Students, Faculty, LabStaff

    // Sender Info
    public Guid SenderUserId { get; init; }
    public string SenderUserName { get; init; } = string.Empty;

    // Status & Timestamps
    public bool IsRead { get; init; }
    public DateTime SentAtUtc { get; init; } = DateTime.UtcNow;
    public DateTime? ExpiresAtUtc { get; init; }
}