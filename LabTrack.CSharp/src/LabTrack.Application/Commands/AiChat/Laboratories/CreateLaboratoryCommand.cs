using MediatR;

namespace LabTrack.Application.Commands.AiChat.Laboratories;

public sealed record CreateLaboratoryCommand : IRequest<Guid>
{
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Building { get; init; } = string.Empty;
    public string FloorLevel { get; init; } = string.Empty;
    public string RoomNumber { get; init; } = string.Empty;
    public int TotalWorkstations { get; init; }
    public TimeSpan OpeningTime { get; init; } = new(8, 0, 0);
    public TimeSpan ClosingTime { get; init; } = new(17, 0, 0);
    public Guid? InChargeStaffId { get; init; }
    public string? Location { get; init; }
    public int Capacity { get; init; }
    public Guid? SupervisorId { get; init; }
}
