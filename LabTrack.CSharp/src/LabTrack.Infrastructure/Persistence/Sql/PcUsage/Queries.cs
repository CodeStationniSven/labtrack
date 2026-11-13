namespace LabTrack.Infrastructure.Persistence.Sql.PcUsage;

public static class Queries
{
    public const string GetActiveSessionsWithDetails = @"
        SELECT 
            p.Id,
            p.AssetId,
            a.Name AS AssetName,
            a.SerialNumber AS AssetSerialNumber,
            p.UserId,
            u.Email AS UserEmail,
            u.FirstName + ' ' + u.LastName AS UserName,
            p.SessionStart,
            DATEDIFF(MINUTE, p.SessionStart, GETUTCDATE()) AS DurationMinutes
        FROM PcUsageLogs p
        INNER JOIN Assets a ON p.AssetId = a.Id
        INNER JOIN Users u ON p.UserId = u.Id
        WHERE p.SessionEnd IS NULL
        ORDER BY p.SessionStart DESC;";

    public const string GetAssetUsageSummaryByPeriod = @"
        SELECT 
            p.AssetId,
            a.Name AS AssetName,
            COUNT(p.Id) AS TotalSessions,
            SUM(DATEDIFF(MINUTE, p.SessionStart, ISNULL(p.SessionEnd, GETUTCDATE()))) AS TotalDurationMinutes,
            AVG(DATEDIFF(MINUTE, p.SessionStart, ISNULL(p.SessionEnd, GETUTCDATE()))) AS AvgSessionDurationMinutes
        FROM PcUsageLogs p
        INNER JOIN Assets a ON p.AssetId = a.Id
        WHERE p.SessionStart >= @StartDate AND p.SessionStart <= @EndDate
        GROUP BY p.AssetId, a.Name
        ORDER BY TotalDurationMinutes DESC;";

    public const string GetUserUsageHistory = @"
        SELECT 
            p.Id,
            p.AssetId,
            a.Name AS AssetName,
            l.Name AS LaboratoryName,
            p.SessionStart,
            p.SessionEnd,
            DATEDIFF(MINUTE, p.SessionStart, ISNULL(p.SessionEnd, GETUTCDATE())) AS DurationMinutes
        FROM PcUsageLogs p
        INNER JOIN Assets a ON p.AssetId = a.Id
        LEFT JOIN Laboratories l ON a.LaboratoryId = l.Id
        WHERE p.UserId = @UserId
        ORDER BY p.SessionStart DESC;";

    public const string GetLongestRunningSessions = @"
        SELECT TOP (@Count)
            p.Id,
            p.AssetId,
            a.Name AS AssetName,
            p.UserId,
            u.Email AS UserEmail,
            p.SessionStart,
            p.SessionEnd,
            DATEDIFF(MINUTE, p.SessionStart, ISNULL(p.SessionEnd, GETUTCDATE())) AS DurationMinutes
        FROM PcUsageLogs p
        INNER JOIN Assets a ON p.AssetId = a.Id
        INNER JOIN Users u ON p.UserId = u.Id
        ORDER BY DurationMinutes DESC;";
}