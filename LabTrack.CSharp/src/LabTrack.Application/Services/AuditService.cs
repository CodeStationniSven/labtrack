using LabTrack.Application.Dtos.Audit;
using LabTrack.Application.Interfaces;

namespace LabTrack.Application.Services;

public class AuditService : IAuditService
{
    private readonly IUnitOfWork _unitOfWork;

    public AuditService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AuditLogDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Audit log ID cannot be empty.", nameof(id));
        }

        return await Task.FromResult<AuditLogDto?>(null);
    }

    public async Task<IReadOnlyList<AuditLogDto>> GetByUserIdAsync(
        Guid userId,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        }

        return await Task.FromResult<IReadOnlyList<AuditLogDto>>(Array.Empty<AuditLogDto>());
    }

    public async Task<IReadOnlyList<AuditLogDto>> GetByEntityAsync(
        string entityName,
        string? entityId = null,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(entityName))
        {
            throw new ArgumentException("Entity name cannot be empty.", nameof(entityName));
        }

        return await Task.FromResult<IReadOnlyList<AuditLogDto>>(Array.Empty<AuditLogDto>());
    }

    public async Task<IReadOnlyList<AuditLogDto>> GetByActionAsync(
        string action,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(action))
        {
            throw new ArgumentException("Action cannot be empty.", nameof(action));
        }

        return await Task.FromResult<IReadOnlyList<AuditLogDto>>(Array.Empty<AuditLogDto>());
    }

    public async Task<IReadOnlyList<AuditLogDto>> GetByDateRangeAsync(
        DateTime startUtc,
        DateTime endUtc,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        if (startUtc > endUtc)
        {
            throw new ArgumentException("Start date must be earlier than or equal to end date.");
        }

        return await Task.FromResult<IReadOnlyList<AuditLogDto>>(Array.Empty<AuditLogDto>());
    }

    public async Task LogActivityAsync(
        CreateAuditLogDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
    
    public async Task<IEnumerable<AuditLogDto>> GetAllAsync(CancellationToken ct = default)
    {
        var logs = await _unitOfWork.Audits.GetAllAsync(ct);
        return logs.Select(log => new AuditLogDto
        {
            Id = log.Id,
            UserId = log.UserId,
            UserName = log.UserName,
            Action = log.Action,
            EntityName = log.EntityName,
            EntityId = log.EntityId,
            OldValues = log.OldValues,
            NewValues = log.NewValues,
            AffectedColumns = null,
            IpAddress = log.IpAddress,
            UserAgent = log.UserAgent,
            TimestampUtc = log.TimestampUtc
        });
    }
}