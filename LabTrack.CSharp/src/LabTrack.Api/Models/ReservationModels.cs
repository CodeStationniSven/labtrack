using System.ComponentModel.DataAnnotations;

namespace LabTrack.Api.Models;

public class CreateReservationRequest
{
    [Required]
    public string LaboratoryName { get; set; } = string.Empty;

    [Required]
    public DateTime ScheduledStartUtc { get; set; }

    [Required]
    public DateTime ScheduledEndUtc { get; set; }

    public string Purpose { get; set; } = "Class Subject";
}

public class ReservationResponse
{
    public string ReservationId { get; set; } = string.Empty;
    public string LaboratoryName { get; set; } = string.Empty;
    public string RequestedByUserId { get; set; } = string.Empty;
    public DateTime ScheduledStartUtc { get; set; }
    public DateTime ScheduledEndUtc { get; set; }
    public string Status { get; set; } = "APPROVED";
}
