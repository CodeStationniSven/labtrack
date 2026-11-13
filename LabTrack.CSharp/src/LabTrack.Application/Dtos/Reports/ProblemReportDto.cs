namespace LabTrack.Application.Dtos.Reports;

public sealed record ProblemReportDto
{
    public Guid Id { get; init; }
    public Guid LaboratoryId { get; init; }
    public string LaboratoryName { get; init; } = string.Empty;
    public Guid? WorkstationId { get; init; }
    public string? WorkstationName { get; init; }
    public Guid? AssetId { get; init; }
    public string? AssetName { get; init; }

    public string Title { get; init; } = string.Empty;
    public string Category { get; init; } = "Hardware";
    public string Severity { get; init; } = "Medium";
    public string Status { get; init; } = "Submitted";
    public string Description { get; init; } = string.Empty;

    public string? StepsToReproduce { get; init; }
    public string? AttachmentUrl { get; init; }
    public string? ResolutionSummary { get; init; }

    public string? ResolutionNotes
    {
        get => ResolutionSummary;
        init => ResolutionSummary = value;
    }

    public Guid ReportedByUserId { get; init; }
    public string ReportedByUserName { get; init; } = string.Empty;

    public Guid ReporterId
    {
        get => ReportedByUserId;
        init => ReportedByUserId = value;
    }

    public DateTime ReportedAtUtc { get; init; } = DateTime.UtcNow;

    public Guid? AssignedToUserId { get; init; }
    public string? AssignedToUserName { get; init; }
    public DateTime? ResolvedAtUtc { get; init; }
}