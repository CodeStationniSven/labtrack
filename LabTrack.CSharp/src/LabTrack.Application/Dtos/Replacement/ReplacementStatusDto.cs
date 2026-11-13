namespace LabTrack.Application.Dtos.Replacement;

public sealed record ReplacementStatusDto
{
    public Guid RequestId { get; init; }
    public string ItemName { get; init; } = string.Empty;
    public string Category { get; init; } = "Peripheral";
    public string CurrentStatus { get; init; } = "Pending"; // e.g., Pending, Approved, Rejected, Fulfilled, Cancelled
    public string Urgency { get; init; } = "Medium";
    
    // Status Metadata & Notes
    public string? DecisionNotes { get; init; }
    public string? ReasonForRejection { get; init; }
    
    // Key Timestamps
    public DateTime RequestedAtUtc { get; init; }
    public DateTime? ReviewedAtUtc { get; init; }
    public DateTime? EstimatedFulfillmentDateUtc { get; init; }
    public DateTime? FulfilledAtUtc { get; init; }

    // User Context
    public Guid RequestedByUserId { get; init; }
    public string RequestedByUserName { get; init; } = string.Empty;
    public Guid? ReviewedByUserId { get; init; }
    public string? ReviewedByUserName { get; init; }
}