using LabTrack.Application.Common.Pagination;

namespace LabTrack.Application.Dtos.Assets;

public sealed record AssetFilterDto : PagedRequest
{
    public string? Category { get; init; }
    public string? Status { get; init; } // e.g., Available, InUse, UnderMaintenance, Retired
    public Guid? LaboratoryId { get; init; }
    public Guid? WorkstationId { get; init; }
    public bool? IsUnderWarranty { get; init; }
    public DateTime? PurchasedAfter { get; init; }
    public DateTime? PurchasedBefore { get; init; }
}