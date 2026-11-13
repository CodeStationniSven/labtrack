using System.ComponentModel.DataAnnotations;

namespace LabTrack.Api.Models;

public class RegisterNetworkDeviceRequest
{
    [Required]
    public string DeviceName { get; set; } = string.Empty;

    [Required]
    public string MacAddress { get; set; } = string.Empty;

    public string IpAddress { get; set; } = string.Empty;
}

public class NetworkDeviceResponse
{
    public string Id { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string Status { get; set; } = "ONLINE";
}
