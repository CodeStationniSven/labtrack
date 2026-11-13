namespace LabTrack.Infrastructure.Realtime;

public static class SignalREventNames
{
    public const string NotificationReceived = "ReceiveNotification";
    public const string PcSessionStarted = "PcSessionStarted";
    public const string PcSessionEnded = "PcSessionEnded";
    public const string ProblemReportCreated = "ProblemReportCreated";
    public const string ProblemReportStatusUpdated = "ProblemReportStatusUpdated";
    public const string ReservationCreated = "ReservationCreated";
    public const string ReservationStatusChanged = "ReservationStatusChanged";
    public const string NetworkIncidentLogged = "NetworkIncidentLogged";
    public const string AiMessageReceived = "AiMessageReceived";
}

public interface ISignalREvent
{
    string EventName { get; }
    object Payload { get; }
    DateTime Timestamp { get; }
}

public record SignalREvent(string EventName, object Payload, DateTime Timestamp) : ISignalREvent;

public static class SignalREventFactory
{
    public static ISignalREvent CreateNotificationEvent(Guid userId, string title, string message, string type)
    {
        return new SignalREvent(
            SignalREventNames.NotificationReceived,
            new { UserId = userId, Title = title, Message = message, Type = type },
            DateTime.UtcNow
        );
    }

    public static ISignalREvent CreatePcSessionStartEvent(Guid assetId, Guid userId, DateTime sessionStart)
    {
        return new SignalREvent(
            SignalREventNames.PcSessionStarted,
            new { AssetId = assetId, UserId = userId, SessionStart = sessionStart },
            DateTime.UtcNow
        );
    }

    public static ISignalREvent CreatePcSessionEndEvent(Guid assetId, Guid userId, DateTime sessionEnd, double durationMinutes)
    {
        return new SignalREvent(
            SignalREventNames.PcSessionEnded,
            new { AssetId = assetId, UserId = userId, SessionEnd = sessionEnd, DurationMinutes = durationMinutes },
            DateTime.UtcNow
        );
    }

    public static ISignalREvent CreateProblemReportCreatedEvent(Guid reportId, Guid assetId, Guid laboratoryId, string priority)
    {
        return new SignalREvent(
            SignalREventNames.ProblemReportCreated,
            new { ReportId = reportId, AssetId = assetId, LaboratoryId = laboratoryId, Priority = priority },
            DateTime.UtcNow
        );
    }

    public static ISignalREvent CreateProblemReportStatusUpdatedEvent(Guid reportId, string newStatus, Guid? technicianId)
    {
        return new SignalREvent(
            SignalREventNames.ProblemReportStatusUpdated,
            new { ReportId = reportId, NewStatus = newStatus, TechnicianId = technicianId },
            DateTime.UtcNow
        );
    }

    public static ISignalREvent CreateReservationCreatedEvent(Guid reservationId, Guid laboratoryId, Guid? assetId, DateTime startTime, DateTime endTime)
    {
        return new SignalREvent(
            SignalREventNames.ReservationCreated,
            new { ReservationId = reservationId, LaboratoryId = laboratoryId, AssetId = assetId, StartTime = startTime, EndTime = endTime },
            DateTime.UtcNow
        );
    }

    public static ISignalREvent CreateReservationStatusChangedEvent(Guid reservationId, string newStatus)
    {
        return new SignalREvent(
            SignalREventNames.ReservationStatusChanged,
            new { ReservationId = reservationId, NewStatus = newStatus },
            DateTime.UtcNow
        );
    }

    public static ISignalREvent CreateNetworkIncidentLoggedEvent(Guid incidentId, Guid laboratoryId, string severity)
    {
        return new SignalREvent(
            SignalREventNames.NetworkIncidentLogged,
            new { IncidentId = incidentId, LaboratoryId = laboratoryId, Severity = severity },
            DateTime.UtcNow
        );
    }

    public static ISignalREvent CreateAiMessageReceivedEvent(Guid sessionId, Guid messageId, string role, string contentSnippet)
    {
        return new SignalREvent(
            SignalREventNames.AiMessageReceived,
            new { SessionId = sessionId, MessageId = messageId, Role = role, ContentSnippet = contentSnippet },
            DateTime.UtcNow
        );
    }
}