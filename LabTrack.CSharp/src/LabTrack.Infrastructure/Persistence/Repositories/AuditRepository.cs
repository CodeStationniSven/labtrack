using LabTrack.Application.Interfaces.Repositories;
using LabTrack.Domain.Entities;
using LabTrack.Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace LabTrack.Infrastructure.Persistence.Repositories;

public class AuditRepository : IAuditRepository
{
    private readonly LabTrackDbContext _context;

    public AuditRepository(LabTrackDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<AuditLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<AuditLog>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs
            .OrderByDescending(a => a.TimestampUtc)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<AuditLog> AddAsync(AuditLog entity, CancellationToken cancellationToken = default)
    {
        await _context.AuditLogs.AddAsync(entity, cancellationToken);
        return entity;
    }

    public Task UpdateAsync(AuditLog entity, CancellationToken cancellationToken = default)
    {
        _context.AuditLogs.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(AuditLog entity, CancellationToken cancellationToken = default)
    {
        _context.AuditLogs.Remove(entity);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<AuditLog>> GetByUserIdAsync(
        Guid userId, int skip = 0, int take = 50,
        CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.TimestampUtc)
            .Skip(skip).Take(take)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditLog>> GetByEntityAsync(
        string entityName, string? entityId = null, int skip = 0, int take = 50,
        CancellationToken cancellationToken = default)
    {
        var query = _context.AuditLogs.Where(a => a.EntityName == entityName);

        if (!string.IsNullOrEmpty(entityId))
            query = query.Where(a => a.EntityId == entityId);

        return await query
            .OrderByDescending(a => a.TimestampUtc)
            .Skip(skip).Take(take)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditLog>> GetByActionAsync(
        string action, int skip = 0, int take = 50,
        CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs
            .Where(a => a.Action == action)
            .OrderByDescending(a => a.TimestampUtc)
            .Skip(skip).Take(take)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditLog>> GetByDateRangeAsync(
        DateTime startUtc, DateTime endUtc, int skip = 0, int take = 100,
        CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs
            .Where(a => a.TimestampUtc >= startUtc && a.TimestampUtc <= endUtc)
            .OrderByDescending(a => a.TimestampUtc)
            .Skip(skip).Take(take)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
