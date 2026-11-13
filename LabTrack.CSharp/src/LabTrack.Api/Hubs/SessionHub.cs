using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace LabTrack.Api.Hubs;

public interface ISessionClient
{
    Task ReceiveSessionStarted(SessionStartedDto session);
    Task ReceiveSessionEnded(SessionEndedDto session);
    Task ReceiveSessionHeartbeat(Guid sessionId, DateTime timestamp);
    Task ReceiveSessionWarning(Guid sessionId, string warningMessage, int remainingMinutes);
    Task ReceiveForceDisconnect(Guid sessionId, string reason);
}

public sealed record SessionStartedDto(
    Guid SessionId,
    Guid WorkstationId,
    string WorkstationNumber,
    Guid UserId,
    string Username,
    DateTime StartedAt,
    DateTime ScheduledEndTime
);

public sealed record SessionEndedDto(
    Guid SessionId,
    Guid WorkstationId,
    Guid UserId,
    DateTime EndedAt,
    string TerminationReason // "USER_LOGOUT", "TIMEOUT", "FORCE_TERMINATED", "INACTIVITY"
);

[Authorize]
public sealed class SessionHub : Hub<ISessionClient>
{
    private readonly ILogger<SessionHub> _logger;

    public SessionHub(ILogger<SessionHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_sessions_{userId}");
        }

        _logger.LogInformation("Client connected to SessionHub: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_sessions_{userId}");
        }

        _logger.LogInformation("Client disconnected from SessionHub: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SubscribeToSession(string sessionId)
    {
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"session_monitor_{sessionId}");
            _logger.LogDebug("Connection {ConnectionId} subscribed to session {SessionId}", Context.ConnectionId, sessionId);
        }
    }

    public async Task UnsubscribeFromSession(string sessionId)
    {
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"session_monitor_{sessionId}");
            _logger.LogDebug("Connection {ConnectionId} unsubscribed from session {SessionId}", Context.ConnectionId, sessionId);
        }
    }

    public async Task SubscribeToWorkstationSessions(string workstationId)
    {
        if (!string.IsNullOrWhiteSpace(workstationId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"workstation_sessions_{workstationId}");
            _logger.LogDebug("Connection {ConnectionId} subscribed to workstation sessions {WorkstationId}", Context.ConnectionId, workstationId);
        }
    }

    public async Task UnsubscribeFromWorkstationSessions(string workstationId)
    {
        if (!string.IsNullOrWhiteSpace(workstationId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"workstation_sessions_{workstationId}");
            _logger.LogDebug("Connection {ConnectionId} unsubscribed from workstation sessions {WorkstationId}", Context.ConnectionId, workstationId);
        }
    }
}