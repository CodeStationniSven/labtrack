namespace LabTrack.Infrastructure.Persistence.Sql.Analytics;

public static class Queries
{
    public const string GetAssetUtilizationStats = @"
        SELECT 
            a.Id AS AssetId,
            a.Name AS AssetName,
            a.SerialNumber,
            l.Name AS LaboratoryName,
            COUNT(p.Id) AS TotalSessions,
            ISNULL(SUM(DATEDIFF(MINUTE, p.SessionStart, ISNULL(p.SessionEnd, GETUTCDATE()))), 0) AS TotalUsageMinutes,
            COUNT(r.Id) AS TotalReservations
        FROM Assets a
        LEFT JOIN Laboratories l ON a.LaboratoryId = l.Id
        LEFT JOIN PcUsageLogs p ON a.Id = p.AssetId
        LEFT JOIN Reservations r ON a.Id = r.AssetId
        GROUP BY a.Id, a.Name, a.SerialNumber, l.Name
        ORDER BY TotalUsageMinutes DESC;";

    public const string GetLaboratoryOverviewStats = @"
        SELECT 
            l.Id AS LaboratoryId,
            l.Name AS LaboratoryName,
            l.Campus,
            COUNT(DISTINCT a.Id) AS TotalAssets,
            COUNT(DISTINCT r.Id) AS ActiveReservations,
            COUNT(DISTINCT ni.Id) AS OpenNetworkIncidents,
            COUNT(DISTINCT pr.Id) AS PendingProblemReports
        FROM Laboratories l
        LEFT JOIN Assets a ON l.Id = a.LaboratoryId
        LEFT JOIN Reservations r ON l.Id = r.LaboratoryId AND r.Status = 'Approved' AND r.EndTime >= GETUTCDATE()
        LEFT JOIN NetworkIncidents ni ON l.Id = ni.LaboratoryId AND ni.Status IN ('Open', 'InProgress')
        LEFT JOIN ProblemReports pr ON l.Id = pr.LaboratoryId AND pr.Status IN ('Open', 'InProgress')
        WHERE l.IsActive = 1
        GROUP BY l.Id, l.Name, l.Campus
        ORDER BY l.Name ASC;";

    public const string GetMaintenanceCostSummaryByPeriod = @"
        SELECT 
            DATEPART(YEAR, fe.ExpenseDate) AS [Year],
            DATEPART(MONTH, fe.ExpenseDate) AS [Month],
            fe.Category,
            COUNT(fe.Id) AS ExpenseCount,
            SUM(fe.Amount) AS TotalCost
        FROM FinancialExpenses fe
        WHERE fe.ExpenseDate >= @StartDate AND fe.ExpenseDate <= @EndDate
        GROUP BY DATEPART(YEAR, fe.ExpenseDate), DATEPART(MONTH, fe.ExpenseDate), fe.Category
        ORDER BY [Year] DESC, [Month] DESC, fe.Category ASC;";

    public const string GetPeakUsageHours = @"
        SELECT 
            DATEPART(WEEKDAY, SessionStart) AS DayOfWeek,
            DATEPART(HOUR, SessionStart) AS HourOfDay,
            COUNT(Id) AS SessionCount
        FROM PcUsageLogs
        WHERE SessionStart >= @StartDate AND SessionStart <= @EndDate
        GROUP BY DATEPART(WEEKDAY, SessionStart), DATEPART(HOUR, SessionStart)
        ORDER BY SessionCount DESC;";
}