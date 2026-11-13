using LabTrack.Application.Interfaces.Repositories;
using LabTrack.Domain.Entities;
using LabTrack.Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace LabTrack.Infrastructure.Persistence.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly LabTrackDbContext _context;

    public NotificationRepository(LabTrackDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    // ─── IGenericRepository<Notification> ───

    public async Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .Include(n => n.User)
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Notification>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .OrderByDescending(n => n.CreatedAtUtc)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Notification> AddAsync(Notification entity, CancellationToken cancellationToken = default)
    {
        await _context.Notifications.AddAsync(entity, cancellationToken);
        return entity;
    }

    public Task UpdateAsync(Notification entity, CancellationToken cancellationToken = default)
    {
        _context.Notifications.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Notification entity, CancellationToken cancellationToken = default)
    {
        _context.Notifications.Remove(entity);
        return Task.CompletedTask;
    }

    // ─── INotificationRepository specific ───

    public async Task<IReadOnlyList<Notification>> GetByUserIdAsync(
        Guid userId,
        bool includeRead = false,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Notifications.Where(n => n.UserId == userId);

        if (!includeRead)
        {
            query = query.Where(n => !n.IsRead);
        }

        return await query
            .OrderByDescending(n => n.CreatedAtUtc)
            .Skip(skip).Take(take)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetUnreadCountByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .CountAsync(cancellationToken);
    }

    public Task<IReadOnlyList<Notification>> GetByRecipientRoleAsync(
        string role,
        bool includeRead = false,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        // Notification has no Role property — return empty
        return Task.FromResult<IReadOnlyList<Notification>>(Array.Empty<Notification>());
    }

    public async Task MarkAsReadAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken);

        if (notification != null && !notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAtUtc = DateTime.UtcNow;
        }
    }

    public async Task MarkAllAsReadByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var unread = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var n in unread)
        {
            n.IsRead = true;
            n.ReadAtUtc = now;
        }
    }

    public async Task DeleteExpiredNotificationsAsync(
        DateTime cutoffUtc,
        CancellationToken cancellationToken = default)
    {
        var expired = await _context.Notifications
            .Where(n => n.CreatedAtUtc < cutoffUtc)
            .ToListAsync(cancellationToken);

        if (expired.Count > 0)
        {
            _context.Notifications.RemoveRange(expired);
        }
    }
}