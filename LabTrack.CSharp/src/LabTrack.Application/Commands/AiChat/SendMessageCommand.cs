using MediatR;

namespace LabTrack.Application.Commands.AiChat;

public sealed record SendMessageCommand
{
    public Guid UserId { get; init; }
    public Guid SessionId { get; init; }
    public string Message { get; init; } = string.Empty;

    public string Question
    {
        get => Message;
        init => Message = value;
    }

    public SendMessageCommand() { }

    public SendMessageCommand(Guid userId, Guid sessionId, string message)
    {
        UserId = userId;
        SessionId = sessionId;
        Message = message;
    }
}