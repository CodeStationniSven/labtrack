namespace LabTrack.Application.Dtos.Analytics;

public sealed record DashboardMetricsDto
{
    // Workstation & PC Sessions
    public int ActiveSessionsCount { get; init; }
    public int TotalWorkstationsCount { get; init; }
    public int OccupiedWorkstationsCount { get; init; }
    public int UnderMaintenanceWorkstationsCount { get; init; }
    public double WorkstationUtilizationPercentage { get; init; }

    // Maintenance & Repair Tickets
    public int OpenRepairTicketsCount { get; init; }
    public int PendingMaintenanceTasksCount { get; init; }
    public int CriticalIssuesCount { get; init; }

    // Reservations & Usage
    public int TodayReservationsCount { get; init; }
    public int UpcomingReservationsCount { get; init; }

    // System Activity & Alerts
    public int UnreadNotificationsCount { get; init; }
    public int ActiveAlertsCount { get; init; }

    // Historical Overview & Metadata
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
    public IReadOnlyCollection<WorkstationStatusSummaryDto> WorkstationSummaries { get; init; } = Array.Empty<WorkstationStatusSummaryDto>();
}

public sealed record WorkstationStatusSummaryDto
{
    public Guid LaboratoryId { get; init; }
    public string LaboratoryName { get; init; } = string.Empty;
    public int TotalStations { get; init; }
    public int ActiveStations { get; init; }
    public int MaintenanceStations { get; init; }
}