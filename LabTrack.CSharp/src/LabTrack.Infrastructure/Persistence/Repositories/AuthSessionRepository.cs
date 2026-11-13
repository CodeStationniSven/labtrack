using LabTrack.Application.Interfaces.Repositories;
using LabTrack.Domain.Entities;
using LabTrack.Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace LabTrack.Infrastructure.Persistence.Repositories;

public class AuthSessionRepository : IAuthSessionRepository
{
    private readonly LabTrackDbContext _db;

    public AuthSessionRepository(LabTrackDbContext db) => _db = db;

    public async Task<AuthSession?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.AuthSessions.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<AuthSession?> GetByRefreshTokenAsync(string refreshToken, CancellationToken ct = default)
        => await _db.AuthSessions
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.RefreshToken == refreshToken, ct);

    public async Task<IReadOnlyList<AuthSession>> GetActiveSessionsByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await _db.AuthSessions
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.RevokedAtUtc == null && x.ExpiresAtUtc > DateTime.UtcNow)
            .ToListAsync(ct);

    public async Task RevokeAllForUserAsync(Guid userId, CancellationToken ct = default)
    {
        var active = await _db.AuthSessions
            .Where(x => x.UserId == userId && x.RevokedAtUtc == null)
            .ToListAsync(ct);
        foreach (var s in active) s.RevokedAtUtc = DateTime.UtcNow;
    }

    public async Task AddAsync(AuthSession entity, CancellationToken ct = default)
        => await _db.AuthSessions.AddAsync(entity, ct);

    public Task UpdateAsync(AuthSession entity, CancellationToken ct = default)
    {
        _db.AuthSessions.Update(entity);
        return Task.CompletedTask;
    }
}
