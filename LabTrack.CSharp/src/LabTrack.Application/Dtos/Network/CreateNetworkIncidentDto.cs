using FluentValidation;

namespace LabTrack.Application.Dtos.Network;

public sealed record CreateNetworkDeviceDto
{
    public string Name { get; init; } = string.Empty;
    public string DeviceType { get; init; } = "Switch";

   
    public string DeviceName
    {
        get => Name;
        init => Name = value;
    }
   

    public string? Manufacturer { get; init; }
    public string? Model { get; init; }

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

public sealed class CreateNetworkDeviceDtoValidator : AbstractValidator<CreateNetworkDeviceDto>
{
    public CreateNetworkDeviceDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Device name is required.")
            .MaximumLength(100).WithMessage("Device name must not exceed 100 characters.");

        RuleFor(x => x.DeviceType)
            .NotEmpty().WithMessage("Device type is required.")
            .MaximumLength(50).WithMessage("Device type must not exceed 50 characters.");

        RuleFor(x => x.IpAddress)
            .MaximumLength(45).WithMessage("IP address must not exceed 45 characters.")
            .When(x => !string.IsNullOrEmpty(x.IpAddress));

        RuleFor(x => x.MacAddress)
            .MaximumLength(50).WithMessage("MAC address must not exceed 50 characters.")
            .When(x => !string.IsNullOrEmpty(x.MacAddress));

        RuleFor(x => x.Location)
            .MaximumLength(100).WithMessage("Location must not exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.Location));

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}