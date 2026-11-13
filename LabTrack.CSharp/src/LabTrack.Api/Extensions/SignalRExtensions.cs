using Microsoft.AspNetCore.SignalR;

namespace LabTrack.Api.Extensions;

public static class SignalRExtensions
{
    public static IServiceCollection AddLabTrackSignalR(this IServiceCollection services)
    {
        services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = true;
            options.MaximumReceiveMessageSize = 1024 * 1024; // 1 MB
            options.HandshakeTimeout = TimeSpan.FromSeconds(15);
            options.KeepAliveInterval = TimeSpan.FromSeconds(10);
        });

        return services;
    }

    public static IEndpointRouteBuilder MapLabTrackHubs(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<LabTrackNotificationHub>("/hubs/notifications");
        endpoints.MapHub<LabTrackTelemetryHub>("/hubs/telemetry");

        return endpoints;
    }
}

public sealed class LabTrackNotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
        }

        await base.OnDisconnectedAsync(exception);
    }
}

public sealed class LabTrackTelemetryHub : Hub
{
    public async Task JoinLaboratoryGroup(string labId)
    {
        if (!string.IsNullOrWhiteSpace(labId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"lab_{labId}");
        }
    }

    public async Task LeaveLaboratoryGroup(string labId)
    {
        if (!string.IsNullOrWhiteSpace(labId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"lab_{labId}");
        }
    }
}