using System.ComponentModel.DataAnnotations;

namespace LabTrack.Api.Models;

public class CreateAssetRequest
{
    [Required]
    public string AssetTag { get; set; } = string.Empty;

    [Required]
    public string Name { get; set; } = string.Empty;

    public string Category { get; set; } = "PC Workstation";
    public string LaboratoryName { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string Specifications { get; set; } = string.Empty;
}

public class UpdateAssetRequest
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "OPERATIONAL";
    public string LaboratoryName { get; set; } = string.Empty;
}

public class AssetResponse
{
    public string Id { get; set; } = string.Empty;
    public string AssetTag { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string LaboratoryName { get; set; } = string.Empty;
    public DateTime DateAcquiredUtc { get; set; }
}
