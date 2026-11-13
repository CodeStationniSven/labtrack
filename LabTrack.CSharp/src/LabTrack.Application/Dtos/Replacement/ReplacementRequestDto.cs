namespace LabTrack.Application.Dtos.Replacement;

public sealed record ReplacementRequestDto
{
    public Guid Id { get; init; }
    public Guid LaboratoryId { get; init; }
    public string LaboratoryName { get; init; } = string.Empty;
    public Guid WorkstationId { get; init; }
    public string WorkstationName { get; init; } = string.Empty;
    public Guid? ExistingAssetId { get; init; }
    public string? ExistingAssetName { get; init; }

    public string ItemName { get; init; } = string.Empty;
    public string Category { get; init; } = "Peripheral";
    public int Quantity { get; init; } = 1;
    public string Urgency { get; init; } = "Medium";
    public string Status { get; init; } = "Pending";
    public string Justification { get; init; } = string.Empty;

    public decimal? EstimatedCost { get; init; }
    public decimal? ApprovedCost { get; init; }
    public string? PreferredVendor { get; init; }

    public string? DecisionNotes { get; init; }
    public Guid? ReviewedByUserId { get; init; }
    public string? ReviewedByUserName { get; init; }
    public DateTime? ReviewedAtUtc { get; init; }

    public Guid RequestedByUserId { get; init; }
    public string RequestedByUserName { get; init; } = string.Empty;
    public DateTime RequestedAtUtc { get; init; } = DateTime.UtcNow;
    public DateTime? FulfilledAtUtc { get; init; }

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

    public Guid InitiatedByUserId
    {
        get => RequestedByUserId;
        init => RequestedByUserId = value;
    }

    public string? Notes
    {
        get => DecisionNotes;
        init => DecisionNotes = value;
    }

    public string TicketNumber
    {
        get => Id == Guid.Empty
            ? string.Empty
            : $"RPL-{RequestedAtUtc:yyyyMMdd}-{Id.ToString()[..6].ToUpper()}";
    }

    public DateTime CreatedAtUtc
    {
        get => RequestedAtUtc;
        init => RequestedAtUtc = value;
    }
}