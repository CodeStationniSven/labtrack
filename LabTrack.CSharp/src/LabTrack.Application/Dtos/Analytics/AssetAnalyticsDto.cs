namespace LabTrack.Application.Dtos.Analytics;

public sealed record AssetAnalyticsDto
{
    public Guid? AssetId { get; init; }
    public string? AssetName { get; init; }
    public Guid? LaboratoryId { get; init; }
    public string? LaboratoryName { get; init; }
    public DateTime StartDateUtc { get; init; }
    public DateTime EndDateUtc { get; init; }
    public int TotalAssetsCount { get; init; }
    public int OperationalAssetsCount { get; init; }
    public int UnderMaintenanceCount { get; init; }
    public int DecommissionedCount { get; init; }
    public int TotalFailures { get; init; }
    public IReadOnlyCollection<AssetFailureBreakdownDto> Breakdown { get; init; } = Array.Empty<AssetFailureBreakdownDto>();
    public DateTime GeneratedAtUtc { get; init; } = DateTime.UtcNow;
}

public sealed record AssetFailureBreakdownDto
{
    public Guid AssetId { get; init; }
    public string AssetName { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public int FailureCount { get; init; }
    public double FailureRate { get; init; }
    public double TotalRepairCost { get; init; }
}