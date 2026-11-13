using MediatR;

namespace LabTrack.Application.Commands.Reservations;

public sealed record CreateReservationCommand : IRequest<Guid>
{
    public Guid UserId { get; init; }
    public Guid LaboratoryId { get; init; }
    public Guid? WorkstationId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Purpose { get; init; } = string.Empty;
    public string ReservationType { get; init; } = "ClassUse";
    public DateTime StartTimeUtc { get; init; }
    public DateTime EndTimeUtc { get; init; }
    public int ExpectedAttendees { get; init; } = 1;
    public string? SpecialRequirements { get; init; }
}