namespace LabTrack.Domain.Entities;

public class QrSession
{
    public Guid Id { get; set; }

    public string Token { get; set; } = string.Empty;

    public Guid UserId { get; set; }

    public Guid? ResourceId { get; set; }

    public string SessionType { get; set; } = string.Empty;
    

    public string Status { get; set; } = "ACTIVE";
  

    public DateTime IssuedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? ConsumedAtUtc { get; set; }

    public string? ConsumedByIp { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
}