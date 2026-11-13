namespace LabTrack.Application.Dtos.Audit;

public sealed record CreateAuditLogDto
{
    public Guid? UserId { get; init; }
    public string Action { get; init; } = string.Empty;
    public string EntityType { get; init; } = string.Empty;
    public Guid? EntityId { get; init; }
    public string? OldValues { get; init; }
    public string? NewValues { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public string? Notes { get; init; }
}