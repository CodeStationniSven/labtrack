using LabTrack.Domain.Entities;

namespace LabTrack.Application.Interfaces.Repositories;

public interface IAuthSessionRepository
{
    Task<AuthSession?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<AuthSession?> GetByRefreshTokenAsync(string refreshToken, CancellationToken ct = default);
    Task<IReadOnlyList<AuthSession>> GetActiveSessionsByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task RevokeAllForUserAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(AuthSession entity, CancellationToken ct = default);
    Task UpdateAsync(AuthSession entity, CancellationToken ct = default);
}
