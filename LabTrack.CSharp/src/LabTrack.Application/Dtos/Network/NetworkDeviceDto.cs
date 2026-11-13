namespace LabTrack.Application.Dtos.Network;

public sealed record NetworkDeviceDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;

   
    public string DeviceName
    {
        get => Name;
        init => Name = value;
    }
    public Guid? WorkstationId { get; init; }
    public DateTime? LastPingUtc { get; init; }
    

    public string DeviceType { get; init; } = "Switch";

    public string? Manufacturer { get; init; }
    public string? Model { get; init; }
    public string? SerialNumber { get; init; }

    public string? IpAddress { get; init; }
    public string? MacAddress { get; init; }
    public string? SubnetMask { get; init; }
    public string? Gateway { get; init; }

    public Guid? LaboratoryId { get; init; }
    public string? LaboratoryName { get; init; }
    public string? Location { get; init; }
    public string? RackPosition { get; init; }

    public string Status { get; init; } = "Active";

    public DateTime? PurchaseDate { get; init; }
    public DateTime? WarrantyExpirationDate { get; init; }
    public DateTime? LastSeenAtUtc { get; init; }

    public string? Notes { get; init; }
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; init; }
}