using LabTrack.Application.Dtos.Audit;

namespace LabTrack.Application.Interfaces;

public interface IAuditService
{
    Task<AuditLogDto?> GetByIdAsync(
        Guid id, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditLogDto>> GetByUserIdAsync(
        Guid userId, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditLogDto>> GetByEntityAsync(
        string entityName, 
        string? entityId = null, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditLogDto>> GetByActionAsync(
        string action, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditLogDto>> GetByDateRangeAsync(
        DateTime startUtc, 
        DateTime endUtc, 
        int skip = 0, 
        int take = 100, 
        CancellationToken cancellationToken = default);

    Task LogActivityAsync(
        CreateAuditLogDto dto, 
        CancellationToken cancellationToken = default);

    Task<IEnumerable<AuditLogDto>> GetAllAsync(CancellationToken ct = default);
}