namespace LabTrack.Application.Dtos.AiChat;

public sealed record AiChatResponseDto
{
    public string Message { get; init; } = string.Empty;
    public string SessionId { get; init; } = string.Empty;
    public string Role { get; init; } = "assistant";
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
    public int TokensUsed { get; init; }
    public IReadOnlyCollection<string> Sources { get; init; } = Array.Empty<string>();
    public IDictionary<string, object>? Metadata { get; init; }
}