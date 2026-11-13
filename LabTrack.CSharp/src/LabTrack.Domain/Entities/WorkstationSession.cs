namespace LabTrack.Domain.Entities;

public class WorkstationSession
{
    public Guid Id { get; set; }

    public Guid WorkstationId { get; set; }

    public Guid UserId { get; set; }

    public DateTime LoginAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? LogoutAtUtc { get; set; }

    public int? DurationMinutes { get; set; }

    public string? IpAddress { get; set; }

    public string? ComputerName { get; set; }

    public string Status { get; set; } = "ACTIVE";
  

    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }



    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
}