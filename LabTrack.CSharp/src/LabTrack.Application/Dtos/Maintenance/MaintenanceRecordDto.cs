namespace LabTrack.Application.Dtos.Maintenance;

public sealed record MaintenanceRecordDto
{
    public Guid Id { get; init; }
    public Guid LaboratoryId { get; init; }
    public string LaboratoryName { get; init; } = string.Empty;
    public Guid? WorkstationId { get; init; }
    public string? WorkstationName { get; init; }
    public Guid? AssetId { get; init; }
    public string? AssetName { get; init; }

    public string Title { get; init; } = string.Empty;
    public string Type { get; init; } = "Preventive"; // e.g., Preventive, Corrective, SoftwareUpdate, Inspection
    public string Status { get; init; } = "Pending"; // e.g., Scheduled, InProgress, Completed, Cancelled
    public string Priority { get; init; } = "Normal"; // e.g., Low, Normal, High, Critical

    // Activity Details
    public string Summary { get; init; } = string.Empty;
    public string? ResolutionNotes { get; init; }
    public string? ReplacedParts { get; init; }
    public decimal TotalCost { get; init; }

    // Timestamps
    public DateTime ScheduledDateUtc { get; init; }
    public DateTime? StartedAtUtc { get; init; }
    public DateTime? CompletedAtUtc { get; init; }

    // Personnel
    public Guid? TechnicianUserId { get; init; }
    public string? TechnicianName { get; init; }
    public Guid LoggedByUserId { get; init; }
    public string LoggedByUserName { get; init; } = string.Empty;
}