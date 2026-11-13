namespace LabTrack.Application.Dtos.AiChat;

public sealed record AiChatRequestDto
{
    public string Message { get; init; } = string.Empty;
    public string? SessionId { get; init; }
    public string? ContextModel { get; init; }
    public IDictionary<string, object>? Metadata { get; init; }
}