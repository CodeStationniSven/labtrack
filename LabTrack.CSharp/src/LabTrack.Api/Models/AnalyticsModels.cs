namespace LabTrack.Api.Models;

public class UsageFilterRequest
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? LaboratoryName { get; set; }
}

public class AnalyticsSummaryResponse
{
    public int TotalSessionsToday { get; set; }
    public int ActiveSessionsNow { get; set; }
    public int PendingProblemReports { get; set; }
    public double SystemUptimePercentage { get; set; }
    public List<LabUsageStatResponse> LabStats { get; set; } = new();
}

public class LabUsageStatResponse
{
    public string LabName { get; set; } = string.Empty;
    public int TotalWorkstations { get; set; }
    public int OccupiedWorkstations { get; set; }
}
