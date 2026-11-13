namespace LabTrack.Application.Dtos.Laboratories;

public sealed record LaboratoryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Building { get; init; } = string.Empty;
    public string FloorLevel { get; init; } = string.Empty;
    public string RoomNumber { get; init; } = string.Empty;
    public int TotalWorkstations { get; init; }
    public int ActiveWorkstationsCount { get; init; }
    public bool IsActive { get; init; } = true;

    // Operating Hours
    public TimeSpan OpeningTime { get; init; }
    public TimeSpan ClosingTime { get; init; }

    // Personnel Context
    public Guid? InChargeStaffId { get; init; }
    public string? InChargeStaffName { get; init; }

    // ─── DINAGDAG ───
    public string? Location { get; init; }
    public int Capacity { get; init; }
    public Guid? SupervisorId { get; init; }
    // ─── END DINAGDAG ───

    // Audit Metadata
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; init; }
}