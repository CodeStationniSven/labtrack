namespace LabTrack.Domain.Enums;

public enum EmailDispatchStatus
{
    Pending = 1,
    Processing = 2,
    Sent = 3,
    Failed = 4,
    Retrying = 5,
    Cancelled = 6
}