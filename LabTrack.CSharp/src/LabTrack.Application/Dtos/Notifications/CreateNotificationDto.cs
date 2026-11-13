namespace LabTrack.Application.Dtos.Notifications;

public sealed record CreateNotificationDto
{
    public Guid UserId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string Type { get; init; } = "Info";
    public string Severity { get; init; } = "Normal";
    public string? ActionUrl { get; init; }
    public string? TargetEntity { get; init; }
    public Guid? TargetEntityId { get; init; }
    public DateTime? ExpiresAtUtc { get; init; }
}