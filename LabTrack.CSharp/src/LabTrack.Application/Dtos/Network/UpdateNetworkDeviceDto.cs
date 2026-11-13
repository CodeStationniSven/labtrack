namespace LabTrack.Application.Dtos.Network;

public sealed record UpdateNetworkDeviceDto
{
    public string? Name { get; init; }

    
    public string? DeviceName
    {
        get => Name;
        init => Name = value;
    }
    public Guid? WorkstationId { get; init; }
   

    public string? DeviceType { get; init; }

    public string? Manufacturer { get; init; }
    public string? Model { get; init; }
    public string? SerialNumber { get; init; }

    public string? IpAddress { get; init; }
    public string? MacAddress { get; init; }
    public string? SubnetMask { get; init; }
    public string? Gateway { get; init; }

    public Guid? LaboratoryId { get; init; }
    public string? Location { get; init; }
    public string? RackPosition { get; init; }

    public string? Status { get; init; }

    public DateTime? PurchaseDate { get; init; }
    public DateTime? WarrantyExpirationDate { get; init; }

    public string? Notes { get; init; }
}