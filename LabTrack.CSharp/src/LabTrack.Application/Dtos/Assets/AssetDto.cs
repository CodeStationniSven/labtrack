namespace LabTrack.Application.Dtos.Assets;

public sealed record AssetDto
{
    public Guid Id { get; init; }
    public string AssetTag { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string? SerialNumber { get; init; }
    public string? Brand { get; init; }
    public string? Model { get; init; }
    public string Status { get; init; } = "Available"; // e.g., Available, InUse, UnderMaintenance, Retired

    // Location & Laboratory Association
    public Guid? LaboratoryId { get; init; }
    public string? LaboratoryName { get; init; }
    public Guid? WorkstationId { get; init; }
    public string? WorkstationName { get; init; }

    // Lifecycle & Warranty Details
    public DateTime? PurchaseDate { get; init; }
    public decimal? PurchaseCost { get; init; }
    public DateTime? WarrantyExpirationDate { get; init; }
    public bool IsUnderWarranty => WarrantyExpirationDate.HasValue && WarrantyExpirationDate.Value >= DateTime.UtcNow.Date;

    // QR Code Metadata
    public string? QrCodeToken { get; init; }

    // Audit Info
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
    public DateTime? LastMaintenanceDateUtc { get; init; }
}