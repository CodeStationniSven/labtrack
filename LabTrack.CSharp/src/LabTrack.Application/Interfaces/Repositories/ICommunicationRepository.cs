using LabTrack.Domain.Entities;

namespace LabTrack.Application.Interfaces.Repositories;

public interface ICommunicationRepository : IGenericRepository<Notification>
{
    Task<IReadOnlyList<Notification>> GetByUserIdAsync(
        Guid userId, 
        bool includeRead = false, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountByUserIdAsync(
        Guid userId, 
        CancellationToken cancellationToken = default);

    Task MarkAsReadAsync(
        Guid notificationId, 
        CancellationToken cancellationToken = default);

    Task MarkAllAsReadByUserIdAsync(
        Guid userId, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Notification>> GetByChannelAsync(
        string channel, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default);
}