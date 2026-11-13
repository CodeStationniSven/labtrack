using System.ComponentModel.DataAnnotations;

namespace LabTrack.Api.Models;

public class GenerateQrRequest
{
    [Required]
    public string TargetId { get; set; } = string.Empty;

    public string Type { get; set; } = "WORKSTATION";
}

public class QrCodeResponse
{
    public string QrId { get; set; } = string.Empty;
    public string CodeValue { get; set; } = string.Empty;
    public string ImageBase64 { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
}
