using LabTrack.Application.Dtos.Notifications;  

namespace LabTrack.Application.Interfaces;

public interface ICommunicationService
{
    Task<NotificationDto?> GetByIdAsync(
        Guid id, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationDto>> GetUserNotificationsAsync(
        Guid userId, 
        bool includeRead = false, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(
        Guid userId, 
        CancellationToken cancellationToken = default);

    Task<NotificationDto> SendNotificationAsync(
        CreateNotificationDto dto, 
        CancellationToken cancellationToken = default);

    Task BroadcastToRoleAsync(
        string role, 
        CreateNotificationDto dto, 
        CancellationToken cancellationToken = default);

    Task MarkAsReadAsync(
        Guid notificationId, 
        CancellationToken cancellationToken = default);

    Task MarkAllAsReadAsync(
        Guid userId, 
        CancellationToken cancellationToken = default);
}