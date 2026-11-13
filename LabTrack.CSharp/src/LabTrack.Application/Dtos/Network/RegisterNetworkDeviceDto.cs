namespace LabTrack.Application.Dtos.Network;

public sealed record RegisterNetworkDeviceDto
{
    public string Name { get; init; } = string.Empty;

    
    public string DeviceName
    {
        get => Name;
        init => Name = value;
    }
    

    public string DeviceType { get; init; } = "Switch";

    public string? Manufacturer { get; init; }
    public string? Model { get; init; }
    public string? SerialNumber { get; init; }

    public string? IpAddress { get; init; }
    public string? MacAddress { get; init; }
    public string? SubnetMask { get; init; }
    public string? Gateway { get; init; }

    public Guid? LaboratoryId { get; init; }

    
    public Guid? WorkstationId { get; init; }
    

    public string? Location { get; init; }
    public string? RackPosition { get; init; }

    public DateTime? PurchaseDate { get; init; }
    public DateTime? WarrantyExpirationDate { get; init; }

    public string? Notes { get; init; }
}