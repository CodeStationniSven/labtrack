namespace LabTrack.Application.Dtos.Qr;

public sealed record QrResponseDto
{
    public string Token { get; init; } = string.Empty;
    public string QrCodeBase64 { get; init; } = string.Empty;
    public string TokenType { get; init; } = string.Empty; // e.g., WorkstationLogin, StudentAttendance, QuickAuth
    public Guid ReferenceId { get; init; }
    public DateTime IssuedAtUtc { get; init; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; init; }
    public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;
}