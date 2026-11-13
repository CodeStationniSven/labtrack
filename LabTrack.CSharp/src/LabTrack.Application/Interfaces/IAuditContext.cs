namespace LabTrack.Application.Interfaces;

public interface IAuditContext
{
    Guid? UserId { get; }
    string? UserEmail { get; }
    string? UserRole { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }
    string? CorrelationId { get; }
}