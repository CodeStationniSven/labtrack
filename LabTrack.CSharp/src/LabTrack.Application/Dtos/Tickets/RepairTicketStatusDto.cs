namespace LabTrack.Application.Dtos.Tickets;

public sealed record RepairTicketStatusDto
{
    public Guid TicketId { get; init; }
    public Guid LaboratoryId { get; init; }
    public string LaboratoryName { get; init; } = string.Empty;
    public Guid WorkstationId { get; init; }
    public string WorkstationName { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;
    public string Category { get; init; } = "Hardware";
    public string Priority { get; init; } = "Medium"; // e.g., Low, Medium, High, Critical
    public string CurrentStatus { get; init; } = "Open"; // e.g., Open, Assigned, InProgress, PendingParts, Resolved, Closed, Cancelled

    // Technician & Resolution Highlights
    public Guid? AssignedTechnicianId { get; init; }
    public string? AssignedTechnicianName { get; init; }
    public string? ResolutionSummary { get; init; }

    // Key Lifecycle Timestamps
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? AssignedAtUtc { get; init; }
    public DateTime? EstimatedCompletionAtUtc { get; init; }
    public DateTime? ResolvedAtUtc { get; init; }
    public DateTime? ClosedAtUtc { get; init; }

    // User Context
    public Guid ReportedByUserId { get; init; }
    public string ReportedByUserName { get; init; } = string.Empty;
}