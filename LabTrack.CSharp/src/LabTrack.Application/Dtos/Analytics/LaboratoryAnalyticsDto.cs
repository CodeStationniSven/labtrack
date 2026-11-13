namespace LabTrack.Application.Dtos.Analytics;

public sealed record LaboratoryAnalyticsDto
{
    public Guid LaboratoryId { get; init; }
    public string LaboratoryName { get; init; } = string.Empty;
    public DateTime StartDateUtc { get; init; }
    public DateTime EndDateUtc { get; init; }
    public int TotalReservations { get; init; }
    public int ActiveWorkstationsCount { get; init; }
    public double UtilizationRatePercentage { get; init; }
    public int TotalSessions { get; init; }
    public double TotalUsageHours { get; init; }
    public int UniqueStudentsServed { get; init; }
    public int PeakConcurrentSessions { get; init; }
    public IReadOnlyCollection<HourlyUsageDto> PeakHours { get; init; } = Array.Empty<HourlyUsageDto>();
    public DateTime GeneratedAtUtc { get; init; } = DateTime.UtcNow;
}

public sealed record HourlyUsageDto
{
    public int Hour { get; init; }
    public int SessionCount { get; init; }
    public double UtilizationPercent { get; init; }
}