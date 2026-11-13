using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace LabTrack.Api.Hubs;

public interface IAiChatClient
{
    Task ReceiveMessageChunk(string chunk);
    Task ReceiveCompleteMessage(AiChatMessageResponse response);
    Task ReceiveError(string errorCode, string errorMessage);
}

public sealed record AiChatMessageResponse(
    Guid MessageId,
    string Role,
    string Content,
    DateTime Timestamp,
    List<string>? ContextSources = null
);

[Authorize]
public sealed class AiChatHub : Hub<IAiChatClient>
{
    private readonly ILogger<AiChatHub> _logger;

    public AiChatHub(ILogger<AiChatHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"chat_user_{userId}");
        }

        _logger.LogInformation("Client connected to AiChatHub: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"chat_user_{userId}");
        }

        _logger.LogInformation("Client disconnected from AiChatHub: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinSessionGroup(string sessionId)
    {
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"session_{sessionId}");
            _logger.LogDebug("Connection {ConnectionId} joined session {SessionId}", Context.ConnectionId, sessionId);
        }
    }

    public async Task LeaveSessionGroup(string sessionId)
    {
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"session_{sessionId}");
            _logger.LogDebug("Connection {ConnectionId} left session {SessionId}", Context.ConnectionId, sessionId);
        }
    }
}