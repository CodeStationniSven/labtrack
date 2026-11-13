using LabTrack.Application.Dtos.Notifications;

namespace LabTrack.Application.Interfaces;

public interface INotificationService
{
    Task<NotificationDto?> GetByIdAsync(
        Guid id, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationDto>> GetByUserIdAsync(
        Guid userId, 
        bool unreadOnly = false, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountByUserIdAsync(
        Guid userId, 
        CancellationToken cancellationToken = default);

    Task<NotificationDto> CreateNotificationAsync(
        CreateNotificationDto dto, 
        CancellationToken cancellationToken = default);

    Task SendToUserAsync(
        Guid userId, 
        string title, 
        string message, 
        string type, 
        CancellationToken cancellationToken = default);

    Task SendToRoleAsync(
        string role, 
        string title, 
        string message, 
        string type, 
        CancellationToken cancellationToken = default);

    Task MarkAsReadAsync(
        Guid id, 
        CancellationToken cancellationToken = default);

    Task MarkAllAsReadAsync(
        Guid userId, 
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid id, 
        CancellationToken cancellationToken = default);

}