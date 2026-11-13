namespace LabTrack.Infrastructure.Persistence.Sql.Reservations;

public static class Queries
{
    public const string GetConflictingReservations = @"
        SELECT 
            r.Id,
            r.AssetId,
            r.LaboratoryId,
            r.ReservedByUserId,
            r.StartTime,
            r.EndTime,
            r.Status
        FROM Reservations r
        WHERE r.Status IN ('Approved', 'Pending')
          AND (@AssetId IS NULL OR r.AssetId = @AssetId)
          AND (@LaboratoryId IS NULL OR r.LaboratoryId = @LaboratoryId)
          AND (@ReservationId IS NULL OR r.Id <> @ReservationId)
          AND r.StartTime < @EndTime 
          AND r.EndTime > @StartTime;";

    public const string GetUpcomingReservationsByUser = @"
        SELECT 
            r.Id,
            r.AssetId,
            a.Name AS AssetName,
            r.LaboratoryId,
            l.Name AS LaboratoryName,
            r.StartTime,
            r.EndTime,
            r.Status,
            r.Purpose
        FROM Reservations r
        LEFT JOIN Assets a ON r.AssetId = a.Id
        LEFT JOIN Laboratories l ON r.LaboratoryId = l.Id
        WHERE r.ReservedByUserId = @UserId
          AND r.EndTime >= GETUTCDATE()
          AND r.Status <> 'Cancelled'
        ORDER BY r.StartTime ASC;";

    public const string GetLaboratoryScheduleByRange = @"
        SELECT 
            r.Id,
            r.LaboratoryId,
            r.AssetId,
            a.Name AS AssetName,
            r.ReservedByUserId,
            u.FirstName + ' ' + u.LastName AS ReservedByName,
            u.Email AS ReservedByEmail,
            r.StartTime,
            r.EndTime,
            r.Status,
            r.Purpose
        FROM Reservations r
        INNER JOIN Users u ON r.ReservedByUserId = u.Id
        LEFT JOIN Assets a ON r.AssetId = a.Id
        WHERE r.LaboratoryId = @LaboratoryId
          AND r.StartTime < @EndDate 
          AND r.EndTime > @StartDate
          AND r.Status IN ('Approved', 'Pending')
        ORDER BY r.StartTime ASC;";

    public const string GetReservationUtilizationStats = @"
        SELECT 
            r.Status,
            COUNT(r.Id) AS TotalReservations,
            ISNULL(SUM(DATEDIFF(MINUTE, r.StartTime, r.EndTime)), 0) AS TotalReservedMinutes
        FROM Reservations r
        WHERE r.StartTime >= @StartDate AND r.EndTime <= @EndDate
          AND (@LaboratoryId IS NULL OR r.LaboratoryId = @LaboratoryId)
        GROUP BY r.Status;";
}