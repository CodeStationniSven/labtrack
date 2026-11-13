using LabTrack.Domain.Entities;

namespace LabTrack.Application.Interfaces.Repositories;

public interface IAuditRepository : IGenericRepository<AuditLog>
{
    Task<IReadOnlyList<AuditLog>> GetByUserIdAsync(
        Guid userId, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditLog>> GetByEntityAsync(
        string entityName, 
        string? entityId = null, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditLog>> GetByActionAsync(
        string action, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditLog>> GetByDateRangeAsync(
        DateTime startUtc, 
        DateTime endUtc, 
        int skip = 0, 
        int take = 100, 
        CancellationToken cancellationToken = default);
}