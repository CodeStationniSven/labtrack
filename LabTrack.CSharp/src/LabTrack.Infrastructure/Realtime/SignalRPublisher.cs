using Microsoft.AspNetCore.SignalR;

namespace LabTrack.Infrastructure.Realtime;

public interface ISignalRPublisher
{
    Task PublishToUserAsync(string userId, ISignalREvent signalREvent, CancellationToken cancellationToken = default);
    Task PublishToUsersAsync(IEnumerable<string> userIds, ISignalREvent signalREvent, CancellationToken cancellationToken = default);
    Task PublishToGroupAsync(string groupName, ISignalREvent signalREvent, CancellationToken cancellationToken = default);
    Task PublishToAllAsync(ISignalREvent signalREvent, CancellationToken cancellationToken = default);
}

public class SignalRPublisher<THub> : ISignalRPublisher where THub : Hub
{
    private readonly IHubContext<THub> _hubContext;

    public SignalRPublisher(IHubContext<THub> hubContext)
    {
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
    }

    public async Task PublishToUserAsync(string userId, ISignalREvent signalREvent, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(userId);
        ArgumentNullException.ThrowIfNull(signalREvent);

        await _hubContext.Clients
            .User(userId)
            .SendAsync(signalREvent.EventName, signalREvent.Payload, cancellationToken);
    }

    public async Task PublishToUsersAsync(IEnumerable<string> userIds, ISignalREvent signalREvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userIds);
        ArgumentNullException.ThrowIfNull(signalREvent);

        var userList = userIds.ToList();
        if (userList.Count == 0) return;

        await _hubContext.Clients
            .Users(userList)
            .SendAsync(signalREvent.EventName, signalREvent.Payload, cancellationToken);
    }

    public async Task PublishToGroupAsync(string groupName, ISignalREvent signalREvent, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(groupName);
        ArgumentNullException.ThrowIfNull(signalREvent);

        await _hubContext.Clients
            .Group(groupName)
            .SendAsync(signalREvent.EventName, signalREvent.Payload, cancellationToken);
    }

    public async Task PublishToAllAsync(ISignalREvent signalREvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(signalREvent);

        await _hubContext.Clients
            .All
            .SendAsync(signalREvent.EventName, signalREvent.Payload, cancellationToken);
    }
}