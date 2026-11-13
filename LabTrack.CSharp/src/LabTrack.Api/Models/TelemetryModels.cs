using System.ComponentModel.DataAnnotations;

namespace LabTrack.Api.Models;

public class PcTelemetryEventRequest
{
    [Required]
    public string WorkstationCode { get; set; } = string.Empty;

    public int HeartbeatBpm { get; set; } = 74;
    public bool IsUserActive { get; set; } = true;
    public int CpuUsagePercent { get; set; } = 15;
    public int MemoryUsagePercent { get; set; } = 40;
}
