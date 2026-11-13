using System.ComponentModel.DataAnnotations;

namespace LabTrack.Api.Models;

public class CreateProblemReportRequest
{
    [Required]
    public string Campus { get; set; } = "JRMSU Main Campus";

    [Required]
    public string LabName { get; set; } = string.Empty;

    [Required]
    public string Category { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public string? WorkstationCode { get; set; }
    public bool HasPhoto { get; set; } = false;
    public string? PhotoBase64 { get; set; }
}

public class ProblemReportResponse
{
    public string ReportId { get; set; } = string.Empty;
    public string Campus { get; set; } = string.Empty;
    public string LabName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "PENDING";
    public string ReportedByStudentId { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
