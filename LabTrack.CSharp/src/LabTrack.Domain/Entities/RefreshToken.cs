namespace LabTrack.Domain.Entities;

public class RefreshToken : Common.BaseEntity
{
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAtUtc { get; set; }
    public string? ReplacedByToken { get; set; }
    public string? RevokedReason { get; set; }
    public string? CreatedByIp { get; set; }
    public string? RevokedByIp { get; set; }

    public DateTime ExpiresAt
    {
        get => ExpiresAtUtc;
        set => ExpiresAtUtc = value;
    }

    public DateTime CreatedAt
    {
        get => CreatedAtUtc;
        set => CreatedAtUtc = value;
    }

    public DateTime? RevokedAt
    {
        get => RevokedAtUtc;
        set => RevokedAtUtc = value;
    }

    public string? ReasonRevoked
    {
        get => RevokedReason;
        set => RevokedReason = value;
    }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;
    public bool IsRevoked => RevokedAtUtc.HasValue;
    public bool IsActive => !IsRevoked && !IsExpired;

    public virtual User? User { get; set; }

    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
}