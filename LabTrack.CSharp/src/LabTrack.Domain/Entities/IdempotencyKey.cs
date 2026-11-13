using LabTrack.Domain.Common;

namespace LabTrack.Domain.Entities;

public class IdempotencyKey : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string RequestName { get; set; } = string.Empty;
    public string ResponsePayload { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;

    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
}