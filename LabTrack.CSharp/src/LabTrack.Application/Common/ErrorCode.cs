namespace LabTrack.Application.Common;

public static class ErrorCode
{
    // General & System
    public const string InternalServerError = "INTERNAL_SERVER_ERROR";
    public const string InvalidArgument = "INVALID_ARGUMENT";
    public const string InvalidOperation = "INVALID_OPERATION";
    public const string ResourceNotFound = "RESOURCE_NOT_FOUND";
    public const string UnauthorizedAccess = "UNAUTHORIZED_ACCESS";
    public const string Forbidden = "FORBIDDEN_ACCESS";

    // Validation
    public const string ValidationFailed = "VALIDATION_FAILED";

    // Auth & Users
    public const string InvalidCredentials = "INVALID_CREDENTIALS";
    public const string UserNotFound = "USER_NOT_FOUND";
    public const string UserAlreadyExists = "USER_ALREADY_EXISTS";
    public const string UserInactive = "USER_INACTIVE";
    public const string TokenExpired = "TOKEN_EXPIRED";
    public const string TokenInvalid = "TOKEN_INVALID";

    // PC Workstations & Sessions
    public const string WorkstationNotFound = "WORKSTATION_NOT_FOUND";
    public const string WorkstationInUse = "WORKSTATION_IN_USE";
    public const string WorkstationMaintenance = "WORKSTATION_UNDER_MAINTENANCE";
    public const string SessionNotFound = "SESSION_NOT_FOUND";
    public const string SessionAlreadyActive = "SESSION_ALREADY_ACTIVE";
    public const string SessionExpired = "SESSION_EXPIRED";

    // Reservations
    public const string ReservationNotFound = "RESERVATION_NOT_FOUND";
    public const string ReservationSlotConflict = "RESERVATION_SLOT_CONFLICT";
    public const string ReservationCancelled = "RESERVATION_CANCELLED";

    // Repair Tickets & Assets
    public const string TicketNotFound = "REPAIR_TICKET_NOT_FOUND";
    public const string AssetNotFound = "ASSET_NOT_FOUND";
    public const string AssetRetired = "ASSET_RETIRED";
}