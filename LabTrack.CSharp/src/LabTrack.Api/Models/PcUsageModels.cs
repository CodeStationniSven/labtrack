using System.ComponentModel.DataAnnotations;

namespace LabTrack.Api.Models;

public class ScanLoginRequest
{
    public string LaboratoryId { get; set; } = "00000000-0000-0000-0000-000000000000";
    public string LaboratoryName { get; set; } = "Main Lab";

    [Required]
    public string WorkstationCode { get; set; } = string.Empty;
}

public class HeartbeatRequest
{
    [Required]
    public string WorkstationCode { get; set; } = string.Empty;
    public string LaboratoryName { get; set; } = "Main Lab";
    public bool IsActive { get; set; } = true;
}

public class PcSessionResponse
{
    public string SessionId { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public string WorkstationCode { get; set; } = string.Empty;
    public string LaboratoryName { get; set; } = string.Empty;
    public DateTime SessionStartUtc { get; set; }
    public DateTime? SessionEndUtc { get; set; }
    public string Status { get; set; } = "ACTIVE";
}
