namespace LabTrack.Application.Dtos.AiChat;

public sealed record CreateAiSessionDto
{
    public Guid UserId { get; init; }
    public string Title { get; init; } = "New Chat Session";
    public string? InitialContextModel { get; init; }
    public IDictionary<string, string>? Metadata { get; init; }
}