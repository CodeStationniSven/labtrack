using System.ComponentModel.DataAnnotations;

namespace LabTrack.Api.Models;

public class CreateScheduleRequest
{
    [Required]
    public string AssetId { get; set; } = string.Empty;

    [Required]
    public DateTime ScheduledDateUtc { get; set; }

    public string TaskDescription { get; set; } = string.Empty;
}

public class MaintenanceScheduleResponse
{
    public string ScheduleId { get; set; } = string.Empty;
    public string AssetId { get; set; } = string.Empty;
    public DateTime ScheduledDateUtc { get; set; }
    public string TaskDescription { get; set; } = string.Empty;
    public string Status { get; set; } = "PENDING";
}
