namespace LabTrack.Application.Dtos.Network;

public sealed record NetworkIncidentDto
{
    public Guid Id { get; init; }
    public Guid LaboratoryId { get; init; }
    public string LaboratoryName { get; init; } = string.Empty;
    public Guid? WorkstationId { get; init; }
    public string? WorkstationName { get; init; }

    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string IncidentType { get; init; } = "Outage"; // e.g., Outage, HighLatency, PacketLoss, IPConflict, UnauthorizedAccess, SwitchPortFailure
    public string Severity { get; init; } = "Medium"; // e.g., Low, Medium, High, Critical
    public string Status { get; init; } = "Open"; // e.g., Open, InProgress, Resolved, Closed

    // Technical Context
    public string? AffectedIpAddress { get; init; }
    public string? AffectedMacAddress { get; init; }
    public string? SwitchPortIdentifier { get; init; }

    // Resolution Metadata
    public string? ResolutionNotes { get; init; }
    public DateTime OccurredAtUtc { get; init; }
    public DateTime? ResolvedAtUtc { get; init; }

    // User & Personnel Context
    public Guid ReportedByUserId { get; init; }
    public string ReportedByUserName { get; init; } = string.Empty;
    public Guid? ResolvedByUserId { get; init; }
    public string? ResolvedByUserName { get; init; }
}