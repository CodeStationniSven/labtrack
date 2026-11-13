namespace LabTrack.Application.Dtos.Analytics;

public sealed record LaboratorySummaryDto
{
    public Guid LaboratoryId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Building { get; init; } = string.Empty;
    public string Floor { get; init; } = string.Empty;
    public bool IsActive { get; init; }

    // Capacity & Workstation Metrics
    public int TotalWorkstations { get; init; }
    public int AvailableWorkstations { get; init; }
    public int OccupiedWorkstations { get; init; }
    public int MaintenanceWorkstations { get; init; }
    public double OccupancyRatePercentage => TotalWorkstations > 0
        ? Math.Round(((double)OccupiedWorkstations / TotalWorkstations) * 100, 2)
        : 0;

    // Maintenance & Incident Summaries
    public int OpenRepairTicketsCount { get; init; }
    public int ScheduledMaintenanceCount { get; init; }

    // Schedule & Reservations
    public int TodayActiveReservationsCount { get; init; }
    public DateTime LastStatusUpdateUtc { get; init; } = DateTime.UtcNow;

    // Assigned Personnel
    public Guid? InChargeUserId { get; init; }
    public string? InChargeUserName { get; init; }
}