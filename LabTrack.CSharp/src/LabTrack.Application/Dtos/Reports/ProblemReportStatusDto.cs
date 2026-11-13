namespace LabTrack.Application.Dtos.Reports;

public sealed record ProblemReportStatusDto
{
    public Guid ReportId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Category { get; init; } = "Hardware"; // e.g., Hardware, Software, Network, Facility, Safety
    public string CurrentStatus { get; init; } = "Submitted"; // e.g., Submitted, UnderReview, InProgress, Resolved, Dismissed
    public string Severity { get; init; } = "Medium"; // e.g., Low, Medium, High, Critical

    // Resolution & Progress Details
    public string? ResolutionSummary { get; init; }
    public string? DismissalReason { get; init; }

    // Timestamps
    public DateTime ReportedAtUtc { get; init; }
    public DateTime? ReviewedAtUtc { get; init; }
    public DateTime? ResolvedAtUtc { get; init; }

    // User Context
    public Guid ReportedByUserId { get; init; }
    public string ReportedByUserName { get; init; } = string.Empty;
    public Guid? AssignedToUserId { get; init; }
    public string? AssignedToUserName { get; init; }
}