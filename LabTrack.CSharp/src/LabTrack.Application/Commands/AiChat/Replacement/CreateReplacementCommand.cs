using MediatR;

namespace LabTrack.Application.Commands.AiChat.Replacement;

public sealed record CreateReplacementCommand : IRequest<Guid>
{
    public Guid LaboratoryId { get; init; }
    public Guid WorkstationId { get; init; }
    public Guid? ExistingAssetId { get; init; }
    public string ItemName { get; init; } = string.Empty;
    public string Category { get; init; } = "Peripheral";
    public int Quantity { get; init; } = 1;
    public string Urgency { get; init; } = "Medium";
    public string Justification { get; init; } = string.Empty;
    public decimal? EstimatedCost { get; init; }
    public string? PreferredVendor { get; init; }

    public Guid OriginalAssetId
    {
        get => ExistingAssetId ?? Guid.Empty;
        init => ExistingAssetId = value == Guid.Empty ? null : value;
    }

    public Guid? ReplacementAssetId { get; init; }

    public string Reason
    {
        get => Justification;
        init => Justification = value;
    }

    public Guid InitiatedByUserId { get; init; }
}
