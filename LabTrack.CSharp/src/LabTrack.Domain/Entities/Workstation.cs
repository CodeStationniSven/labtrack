namespace LabTrack.Domain.Entities;

public class Workstation : Common.BaseEntity
{
    public Guid LaboratoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Hostname { get; set; }
    public string? IpAddress { get; set; }
    public string? MacAddress { get; set; }
    public string? OperatingSystem { get; set; }
    public string? CpuModel { get; set; }
    public string? RamSize { get; set; }
    public string? StorageSize { get; set; }
    public string Status { get; set; } = "Available";
    public bool IsActive { get; set; } = true;
    public DateTime? LastSeenAtUtc { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
}