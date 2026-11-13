using MediatR;

namespace LabTrack.Application.Commands.Usage;

public sealed record ScanLoginCommand : IRequest<Guid>
{
    public string QrToken { get; init; } = string.Empty;
    public Guid WorkstationId { get; init; }
    public Guid StudentId { get; init; }
    public string? ClientIpAddress { get; init; }

    public string QrData
    {
        get => QrToken;
        init => QrToken = value;
    }

    public Guid UserId
    {
        get => StudentId;
        init => StudentId = value;
    }
}