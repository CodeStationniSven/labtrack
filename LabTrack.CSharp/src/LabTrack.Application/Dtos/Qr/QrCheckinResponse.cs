namespace LabTrack.Application.Dtos.Qr;

public sealed record QrCheckinResponse
{
    public bool IsSuccess { get; init; }
    public string Message { get; init; } = string.Empty;

    // Session / Attendance Details
    public Guid? SessionId { get; init; }
    public Guid StudentId { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public string StudentNumber { get; init; } = string.Empty;

    // Workstation & Laboratory Details
    public Guid WorkstationId { get; init; }
    public string WorkstationName { get; init; } = string.Empty;
    public Guid LaboratoryId { get; init; }
    public string LaboratoryName { get; init; } = string.Empty;

    // Timestamps
    public DateTime CheckedInAtUtc { get; init; } = DateTime.UtcNow;
    public DateTime? ExpectedCheckOutAtUtc { get; init; }
}