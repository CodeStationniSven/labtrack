namespace LabTrack.Application.Dtos.AiChat;

public sealed record AiMessageDto
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string SessionId { get; init; } = string.Empty;
    public string Role { get; init; } = "user"; // "user", "assistant", or "system"
    public string Content { get; init; } = string.Empty;
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
    public int? Tokens { get; init; }
    public IReadOnlyCollection<string>? Sources { get; init; }
}