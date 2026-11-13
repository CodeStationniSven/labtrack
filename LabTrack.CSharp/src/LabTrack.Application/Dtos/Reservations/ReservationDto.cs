namespace LabTrack.Application.Dtos.Reservations;

public sealed record ReservationDto
{
    public Guid Id { get; init; }
    public Guid LaboratoryId { get; init; }
    public string LaboratoryName { get; init; } = string.Empty;
    public Guid? WorkstationId { get; init; }
    public string? WorkstationName { get; init; }

    public string Title { get; init; } = string.Empty;
    public string Purpose { get; init; } = string.Empty;
    public string ReservationType { get; init; } = "ClassUse";
    public string Status { get; init; } = "Pending";

    public DateTime StartTimeUtc { get; init; }
    public DateTime EndTimeUtc { get; init; }
    public double DurationHours => (EndTimeUtc - StartTimeUtc).TotalHours;
    public int ExpectedAttendees { get; init; } = 1;
    public string? SpecialRequirements { get; init; }

    public string? DecisionNotes { get; init; }
    public Guid? ReviewedByUserId { get; init; }
    public string? ReviewedByUserName { get; init; }
    public DateTime? ReviewedAtUtc { get; init; }

    public Guid ReservedByUserId { get; init; }
    public string ReservedByUserName { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;

    public Guid UserId
    {
        get => ReservedByUserId;
        init => ReservedByUserId = value;
    }
}