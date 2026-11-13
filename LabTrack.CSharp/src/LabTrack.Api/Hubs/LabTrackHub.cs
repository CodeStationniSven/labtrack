using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace LabTrack.Api.Hubs;

public interface ILabTrackClient
{
    Task ReceiveTelemetryUpdate(TelemetryUpdateDto telemetry);
    Task ReceiveNotification(NotificationDto notification);
    Task ReceiveWorkstationStatusChanged(Guid workstationId, string status);
    Task ReceiveSessionEvent(Guid sessionId, string eventType, DateTime timestamp);
}

public sealed record TelemetryUpdateDto(
    Guid WorkstationId,
    double CpuUsagePercentage,
    double MemoryUsagePercentage,
    double DiskUsagePercentage,
    double TemperatureCelsius,
    DateTime Timestamp
);

public sealed record NotificationDto(
    Guid Id,
    string Title,
    string Message,
    string Severity, // "INFO", "WARNING", "CRITICAL"
    DateTime CreatedAt
);

[Authorize]
public sealed class LabTrackHub : Hub<ILabTrackClient>
{
    private readonly ILogger<LabTrackHub> _logger;

    public LabTrackHub(ILogger<LabTrackHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
        }

        _logger.LogInformation("Client connected to LabTrackHub: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
        }

        _logger.LogInformation("Client disconnected from LabTrackHub: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinLabRoom(string labId)
    {
        if (!string.IsNullOrWhiteSpace(labId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"lab_{labId}");
            _logger.LogDebug("Connection {ConnectionId} joined lab room {LabId}", Context.ConnectionId, labId);
        }
    }

    public async Task LeaveLabRoom(string labId)
    {
        if (!string.IsNullOrWhiteSpace(labId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"lab_{labId}");
            _logger.LogDebug("Connection {ConnectionId} left lab room {LabId}", Context.ConnectionId, labId);
        }
    }

    public async Task JoinWorkstationRoom(string workstationId)
    {
        if (!string.IsNullOrWhiteSpace(workstationId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"workstation_{workstationId}");
            _logger.LogDebug("Connection {ConnectionId} joined workstation room {WorkstationId}", Context.ConnectionId, workstationId);
        }
    }

    public async Task LeaveWorkstationRoom(string workstationId)
    {
        if (!string.IsNullOrWhiteSpace(workstationId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"workstation_{workstationId}");
            _logger.LogDebug("Connection {ConnectionId} left workstation room {WorkstationId}", Context.ConnectionId, workstationId);
        }
    }
}