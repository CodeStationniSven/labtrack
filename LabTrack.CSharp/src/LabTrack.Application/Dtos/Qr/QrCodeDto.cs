namespace LabTrack.Application.Dtos.Qr;

public sealed record QrCodeDto
{
    public Guid Id { get; init; }

    public string Token { get; init; } = string.Empty;
    public string TokenType { get; init; } = "WorkstationLogin";

    public Guid ReferenceId { get; init; }

    public bool IsActive { get; init; } = true;
    public bool IsUsed { get; init; } = false;
    public DateTime? UsedAtUtc { get; init; }

    public DateTime GeneratedAtUtc { get; init; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; init; }
    public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;
    public bool IsValid => IsActive && !IsUsed && !IsExpired;

    public Guid GeneratedByUserId { get; init; }
    public string? AdditionalPayload { get; init; }

    public string EntityType
    {
        get => TokenType;
        init => TokenType = value;
    }

    public Guid EntityId
    {
        get => ReferenceId;
        init => ReferenceId = value;
    }

    public string CodeData
    {
        get => Token;
        init => Token = value;
    }

    public string? Base64Image { get; init; }

    public DateTime CreatedAtUtc
    {
        get => GeneratedAtUtc;
        init => GeneratedAtUtc = value;
    }
}