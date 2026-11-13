namespace LabTrack.Application.Dtos.Analytics;

public sealed record PcUsageAnalyticsDto
{
    public Guid? LaboratoryId { get; init; }
    public string? LaboratoryName { get; init; }
    public DateTime StartDateUtc { get; init; }
    public DateTime EndDateUtc { get; init; }
    public int TotalSessionsCount { get; init; }
    public double AverageSessionMinutes { get; init; }
    public double TotalUsageHours { get; init; }
    public int UniqueWorkstations { get; init; }
    public int UniqueStudentsServed { get; init; }
    public IReadOnlyCollection<WorkstationUsageDto> WorkstationUsage { get; init; } = Array.Empty<WorkstationUsageDto>();
    public DateTime GeneratedAtUtc { get; init; } = DateTime.UtcNow;
}

public sealed record WorkstationUsageDto
{
    public Guid WorkstationId { get; init; }
    public string WorkstationName { get; init; } = string.Empty;
    public string LaboratoryName { get; init; } = string.Empty;
    public int SessionCount { get; init; }
    public double TotalUsageHours { get; init; }
    public double AverageSessionMinutes { get; init; }
    public DateTime LastUsedAtUtc { get; init; }
}