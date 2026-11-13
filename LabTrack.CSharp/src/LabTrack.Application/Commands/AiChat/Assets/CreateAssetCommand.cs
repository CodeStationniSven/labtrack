using MediatR;

namespace LabTrack.Application.Commands.AiChat.Assets;

public sealed record CreateAssetCommand : IRequest<Guid>
{
    public string AssetTag { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string? SerialNumber { get; init; }
    public string? Brand { get; init; }
    public string? Model { get; init; }
    public string Status { get; init; } = "Available";
    public Guid? LaboratoryId { get; init; }
    public Guid? WorkstationId { get; init; }
    public DateTime? PurchaseDate { get; init; }
    public decimal? PurchaseCost { get; init; }
    public DateTime? WarrantyExpirationDate { get; init; }
}
