namespace LabTrack.Application.Dtos.Tickets;

public sealed record RepairTicketDto
{
    public Guid Id { get; init; }
    public Guid LaboratoryId { get; init; }
    public string LaboratoryName { get; init; } = string.Empty;
    public Guid WorkstationId { get; init; }
    public string WorkstationName { get; init; } = string.Empty;
    public Guid? AssetId { get; init; }
    public string? AssetName { get; init; }

    public string Title { get; init; } = string.Empty;
    public string IssueDescription { get; init; } = string.Empty;
    public string Category { get; init; } = "Hardware";
    public string Priority { get; init; } = "Medium";
    public string Status { get; init; } = "Open";

    public string? StepsToReproduce { get; init; }
    public string? AttachmentUrl { get; init; }
    public string? AssignmentNotes { get; init; }
    public string? ResolutionSummary { get; init; }

    public Guid ReportedByUserId { get; init; }
    public string ReportedByUserName { get; init; } = string.Empty;
    public Guid? AssignedTechnicianId { get; init; }
    public string? AssignedTechnicianName { get; init; }

    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
    public DateTime? AssignedAtUtc { get; init; }
    public DateTime? EstimatedCompletionAtUtc { get; init; }
    public DateTime? ResolvedAtUtc { get; init; }
    public DateTime? ClosedAtUtc { get; init; }

    public string TicketNumber
    {
        get => Id == Guid.Empty
            ? string.Empty
            : $"REP-{CreatedAtUtc:yyyyMMdd}-{Id.ToString()[..6].ToUpper()}";
    }

    public string ProblemDescription
    {
        get => IssueDescription;
        init => IssueDescription = value;
    }

    public string? Notes
    {
        get => ResolutionSummary;
        init => ResolutionSummary = value;
    }
}