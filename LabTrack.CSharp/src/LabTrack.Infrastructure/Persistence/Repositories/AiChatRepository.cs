using LabTrack.Application.Interfaces.Repositories;
using LabTrack.Domain.Entities;
using LabTrack.Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace LabTrack.Infrastructure.Persistence.Repositories;

public class AiChatRepository : IAiChatRepository
{
    private readonly LabTrackDbContext _context;

    public AiChatRepository(LabTrackDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    // ─── Session methods ───

    public async Task<AiChatSession?> GetSessionByIdAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await _context.AiChatSessions
            .Include(s => s.Messages)
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
    }

    public async Task<IEnumerable<AiChatSession>> GetSessionsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.AiChatSessions
            .Include(s => s.Messages)
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task AddSessionAsync(AiChatSession session, CancellationToken cancellationToken = default)
    {
        await _context.AiChatSessions.AddAsync(session, cancellationToken);
    }

    public void UpdateSession(AiChatSession session)
    {
        _context.AiChatSessions.Update(session);
    }

    public void DeleteSession(AiChatSession session)
    {
        _context.AiChatSessions.Remove(session);
    }

    // ─── Message methods ───

    public async Task AddMessageAsync(AiChatMessage message, CancellationToken cancellationToken = default)
    {
        await _context.AiChatMessages.AddAsync(message, cancellationToken);
    }

    public async Task<IEnumerable<AiChatMessage>> GetMessagesBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await _context.AiChatMessages
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    // ─── IGenericRepository<AiChatMessage> ───

    public async Task<AiChatMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.AiChatMessages
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<AiChatMessage>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AiChatMessages
            .OrderBy(m => m.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<AiChatMessage> AddAsync(AiChatMessage entity, CancellationToken cancellationToken = default)
    {
        await _context.AiChatMessages.AddAsync(entity, cancellationToken);
        return entity;
    }

    public async Task UpdateAsync(AiChatMessage entity, CancellationToken cancellationToken = default)
    {
        _context.AiChatMessages.Update(entity);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(AiChatMessage entity, CancellationToken cancellationToken = default)
    {
        _context.AiChatMessages.Remove(entity);
        await Task.CompletedTask;
    }

    // ─── IAiChatRepository specific ───

    public async Task<IReadOnlyList<AiChatMessage>> GetChatHistoryBySessionIdAsync(
        Guid sessionId,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        return await _context.AiChatMessages
            .Where(m => m.SessionId == sessionId)
            .OrderByDescending(m => m.CreatedAtUtc)
            .Take(limit)
            .OrderBy(m => m.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AiChatMessage>> GetUserChatHistoryAsync(
        Guid userId,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        return await _context.AiChatMessages
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteSessionHistoryAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var messages = await _context.AiChatMessages
            .Where(m => m.SessionId == sessionId)
            .ToListAsync(cancellationToken);

        if (messages.Count > 0)
        {
            _context.AiChatMessages.RemoveRange(messages);
        }
    }
}