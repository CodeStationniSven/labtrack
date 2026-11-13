namespace LabTrack.Application.Dtos.Communications;

public sealed record SendMessageDto
{
    public Guid SenderId { get; init; }
    public Guid? RecipientId { get; init; }
    public Guid? ChannelId { get; init; }
    public string Content { get; init; } = string.Empty;
    public string MessageType { get; init; } = "Text";
}