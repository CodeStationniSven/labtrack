namespace LabTrack.Infrastructure.Persistence.Sql.Audit;

public static class Queries
{
    public const string GetAuditTrailByEntity = @"
        SELECT 
            a.Id,
            a.UserId,
            u.Email AS UserEmail,
            u.FirstName + ' ' + u.LastName AS UserName,
            a.Action,
            a.EntityName,
            a.EntityId,
            a.OldValues,
            a.NewValues,
            a.IpAddress,
            a.CreatedAt
        FROM AuditLogs a
        LEFT JOIN Users u ON a.UserId = u.Id
        WHERE a.EntityName = @EntityName AND a.EntityId = @EntityId
        ORDER BY a.CreatedAt DESC;";

    public const string GetUserActivityLog = @"
        SELECT 
            a.Id,
            a.UserId,
            a.Action,
            a.EntityName,
            a.EntityId,
            a.IpAddress,
            a.CreatedAt
        FROM AuditLogs a
        WHERE a.UserId = @UserId
          AND (@StartDate IS NULL OR a.CreatedAt >= @StartDate)
          AND (@EndDate IS NULL OR a.CreatedAt <= @EndDate)
        ORDER BY a.CreatedAt DESC;";

    public const string GetRecentSystemActivity = @"
        SELECT TOP (@Count)
            a.Id,
            a.UserId,
            u.Email AS UserEmail,
            a.Action,
            a.EntityName,
            a.EntityId,
            a.CreatedAt
        FROM AuditLogs a
        LEFT JOIN Users u ON a.UserId = u.Id
        ORDER BY a.CreatedAt DESC;";

    public const string GetActionCountsByEntity = @"
        SELECT 
            a.EntityName,
            a.Action,
            COUNT(1) AS ActionCount
        FROM AuditLogs a
        WHERE a.CreatedAt >= @StartDate AND a.CreatedAt <= @EndDate
        GROUP BY a.EntityName, a.Action
        ORDER BY ActionCount DESC;";
}