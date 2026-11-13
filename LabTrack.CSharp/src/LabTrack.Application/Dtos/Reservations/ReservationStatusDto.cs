namespace LabTrack.Application.Dtos.Reservations;

public sealed record ReservationStatusDto
{
    public Guid ReservationId { get; init; }
    public Guid LaboratoryId { get; init; }
    public string LaboratoryName { get; init; } = string.Empty;
    public Guid? WorkstationId { get; init; }
    public string? WorkstationName { get; init; }

    public string Title { get; init; } = string.Empty;
    public string ReservationType { get; init; } = "ClassUse"; // e.g., ClassUse, IndividualStudy, Maintenance, Research, Exam
    public string CurrentStatus { get; init; } = "Pending"; // e.g., Pending, Approved, Rejected, Cancelled, Completed

    // Timing Metadata
    public DateTime StartTimeUtc { get; init; }
    public DateTime EndTimeUtc { get; init; }
    public bool IsActiveNow => DateTime.UtcNow >= StartTimeUtc && DateTime.UtcNow <= EndTimeUtc;

    // Review / Approval Details
    public string? DecisionNotes { get; init; }
    public string? CancellationReason { get; init; }

    // User & Audit Context
    public Guid ReservedByUserId { get; init; }
    public string ReservedByUserName { get; init; } = string.Empty;
    public Guid? ReviewedByUserId { get; init; }
    public string? ReviewedByUserName { get; init; }
    public DateTime? ReviewedAtUtc { get; init; }
}