namespace LabTrack.Application.Dtos.Communications;

public sealed record ChatMessageDto
{
    public Guid Id { get; init; }
    public Guid SenderId { get; init; }
    public string SenderName { get; init; } = string.Empty;
    public Guid? RecipientId { get; init; }
    public string? RecipientName { get; init; }
    public Guid? ChannelId { get; init; }
    public string? ChannelName { get; init; }
    public string Content { get; init; } = string.Empty;
    public string MessageType { get; init; } = "Text";
    public bool IsRead { get; init; }
    public DateTime? ReadAtUtc { get; init; }
    public DateTime SentAtUtc { get; init; } = DateTime.UtcNow;
}