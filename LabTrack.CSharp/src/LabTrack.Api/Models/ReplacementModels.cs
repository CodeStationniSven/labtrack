using System.ComponentModel.DataAnnotations;

namespace LabTrack.Api.Models;

public class CreateReplacementRequest
{
    [Required]
    public string TicketId { get; set; } = string.Empty;

    [Required]
    public string ComponentName { get; set; } = string.Empty;

    public decimal EstimatedCost { get; set; }
}

public class ReplacementResponse
{
    public string ReplacementId { get; set; } = string.Empty;
    public string TicketId { get; set; } = string.Empty;
    public string ComponentName { get; set; } = string.Empty;
    public decimal EstimatedCost { get; set; }
    public string Status { get; set; } = "PENDING_APPROVAL";
}
