using System.ComponentModel.DataAnnotations;

namespace LabTrack.Api.Models;

public class CreateRepairTicketRequest
{
    [Required]
    public string AssetId { get; set; } = string.Empty;

    [Required]
    public string IssueDescription { get; set; } = string.Empty;

    public string Priority { get; set; } = "MEDIUM";
}

public class RepairTicketResponse
{
    public string TicketId { get; set; } = string.Empty;
    public string AssetId { get; set; } = string.Empty;
    public string IssueDescription { get; set; } = string.Empty;
    public string Priority { get; set; } = "MEDIUM";
    public string Status { get; set; } = "OPEN";
    public DateTime CreatedAtUtc { get; set; }
}
