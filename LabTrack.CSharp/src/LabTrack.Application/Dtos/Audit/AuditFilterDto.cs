using LabTrack.Application.Common.Pagination;

namespace LabTrack.Application.Dtos.Audit;

public sealed record AuditFilterDto : PagedRequest
{
    public Guid? UserId { get; init; }
    public string? Action { get; init; } // e.g., Create, Update, Delete, Login, Scan
    public string? EntityName { get; init; } // e.g., Asset, Workstation, Reservation
    public string? EntityId { get; init; }
    public DateTime? FromDateUtc { get; init; }
    public DateTime? ToDateUtc { get; init; }
    public string? IpAddress { get; init; }
}