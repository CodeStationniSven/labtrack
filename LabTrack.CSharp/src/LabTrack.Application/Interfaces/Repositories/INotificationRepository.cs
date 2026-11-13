using LabTrack.Domain.Entities;

namespace LabTrack.Application.Interfaces.Repositories;

public interface INotificationRepository : IGenericRepository<Notification>
{
    Task<IReadOnlyList<Notification>> GetByUserIdAsync(
        Guid userId, 
        bool unreadOnly = false, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountByUserIdAsync(
        Guid userId, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Notification>> GetByRecipientRoleAsync(
        string role, 
        bool unreadOnly = false, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default);

    Task MarkAsReadAsync(
        Guid notificationId, 
        CancellationToken cancellationToken = default);

    Task MarkAllAsReadByUserIdAsync(
        Guid userId, 
        CancellationToken cancellationToken = default);

    Task DeleteExpiredNotificationsAsync(
        DateTime expiryThresholdUtc, 
        CancellationToken cancellationToken = default);
}