namespace LabTrack.Application.Dtos.Usage;

public sealed record PcUsageDto
{
    public Guid SessionId { get; init; }
    public Guid WorkstationId { get; init; }
    public string WorkstationName { get; init; } = string.Empty;
    public Guid LaboratoryId { get; init; }
    public string LaboratoryName { get; init; } = string.Empty;

    // Student Information
    public Guid StudentId { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public string StudentNumber { get; init; } = string.Empty;

    // Session Timing & Telemetry
    public DateTime StartedAtUtc { get; init; }
    public DateTime? EndedAtUtc { get; init; }
    public double TotalUsageMinutes => ((EndedAtUtc ?? DateTime.UtcNow) - StartedAtUtc).TotalMinutes;
    public bool IsActive => !EndedAtUtc.HasValue;

    // Condition & Telemetry Status
    public string SystemStatus { get; init; } = "Active"; // e.g., Active, Idle, Locked, Ended
    public string? WorkstationCondition { get; init; } = "Good";
    public DateTime LastHeartbeatAtUtc { get; init; } = DateTime.UtcNow;
}