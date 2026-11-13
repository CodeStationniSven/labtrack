using System.Text.Json;
using LabTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;

namespace LabTrack.Infrastructure.Audit;

public interface IAuditWriter
{
    Task WriteAuditLogsAsync(DbContext context, CancellationToken cancellationToken = default);
}

public class AuditWriter : IAuditWriter
{
    private readonly IAuditContextAccessor _auditContextAccessor;
    private readonly ILogger<AuditWriter> _logger;

    public AuditWriter(
        IAuditContextAccessor auditContextAccessor,
        ILogger<AuditWriter> logger)
    {
        _auditContextAccessor = auditContextAccessor;
        _logger = logger;
    }

    public async Task WriteAuditLogsAsync(DbContext context, CancellationToken cancellationToken = default)
    {
        var entries = context.ChangeTracker.Entries()
            .Where(e => e.Entity is not AuditLog && 
                        (e.State == EntityState.Added || 
                         e.State == EntityState.Modified || 
                         e.State == EntityState.Deleted))
            .ToList();

        if (entries.Count == 0)
        {
            return;
        }

        var auditLogs = new List<AuditLog>();
        var now = DateTime.UtcNow;
        var userId = _auditContextAccessor.UserId;
        var userName = _auditContextAccessor.UserName;
        var ipAddress = _auditContextAccessor.IpAddress;
        var userAgent = _auditContextAccessor.UserAgent;
        var correlationId = _auditContextAccessor.CorrelationId;

        foreach (var entry in entries)
        {
            var auditLog = CreateAuditLog(entry, now, userId, userName, ipAddress, userAgent, correlationId);
            if (auditLog is not null)
            {
                auditLogs.Add(auditLog);
            }
        }

        if (auditLogs.Count > 0)
        {
            await context.Set<AuditLog>().AddRangeAsync(auditLogs, cancellationToken);
            _logger.LogInformation("Generated {Count} audit log entries for current change tracker transaction.", auditLogs.Count);
        }
    }

    private static AuditLog? CreateAuditLog(
        EntityEntry entry,
        DateTime timestamp,
        string? userId,
        string? userName,
        string? ipAddress,
        string? userAgent,
        string? correlationId)
    {
        var entityName = entry.Entity.GetType().Name;
        var primaryKey = GetPrimaryKeyValue(entry);

        var oldValues = new Dictionary<string, object?>();
        var newValues = new Dictionary<string, object?>();
        var action = entry.State switch
        {
            EntityState.Added => "Create",
            EntityState.Modified => "Update",
            EntityState.Deleted => "Delete",
            _ => "Unknown"
        };

        foreach (var property in entry.Properties)
        {
            if (property.Metadata.IsShadowProperty()) continue;

            var propertyName = property.Metadata.Name;

            switch (entry.State)
            {
                case EntityState.Added:
                    newValues[propertyName] = property.CurrentValue;
                    break;

                case EntityState.Deleted:
                    oldValues[propertyName] = property.OriginalValue;
                    break;

                case EntityState.Modified:
                    if (property.IsModified)
                    {
                        oldValues[propertyName] = property.OriginalValue;
                        newValues[propertyName] = property.CurrentValue;
                    }
                    break;
            }
        }

        return new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityName = entityName,
            EntityId = primaryKey,
            Action = action,
            OldValues = oldValues.Count > 0 ? JsonSerializer.Serialize(oldValues) : null,
            NewValues = newValues.Count > 0 ? JsonSerializer.Serialize(newValues) : null,
            UserId = Guid.TryParse(userId, out var parsedUserId) ? parsedUserId : null,
            UserName = userName,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CorrelationId = correlationId,
            TimestampUtc = timestamp, 
        };
    }

    private static string GetPrimaryKeyValue(EntityEntry entry)
    {
        var keyProperty = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
        return keyProperty?.CurrentValue?.ToString() ?? string.Empty;
    }
}