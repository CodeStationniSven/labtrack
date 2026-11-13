namespace LabTrack.Application.Dtos.Workstations;

public sealed record WorkstationDto
{
    public Guid Id { get; init; }
    public Guid LaboratoryId { get; init; }
    public string LaboratoryName { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;
    public string? Code { get; init; }
    public string? Hostname { get; init; }
    public string? IpAddress { get; init; }
    public string? MacAddress { get; init; }

    public string? OperatingSystem { get; init; }
    public string? CpuModel { get; init; }
    public string? RamSize { get; init; }
    public string? StorageSize { get; init; }

    public string Status { get; init; } = "Available";
    // e.g., Available, InUse, UnderMaintenance, Retired

    public bool IsActive { get; init; } = true;
    public DateTime? LastSeenAtUtc { get; init; }

    public string? Notes { get; init; }

    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; init; }
}