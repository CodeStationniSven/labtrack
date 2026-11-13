using MediatR;

namespace LabTrack.Application.Commands.AiChat.Qr;

public sealed record GenerateQrCommand : IRequest<Guid>
{
    public string TokenType { get; init; } = "WorkstationLogin";
    public Guid ReferenceId { get; init; }
    public int ExpirationMinutes { get; init; } = 15;
    public string? AdditionalPayload { get; init; }

    public string EntityType
    {
        get => TokenType;
        init => TokenType = value;
    }

    public Guid EntityId
    {
        get => ReferenceId;
        init => ReferenceId = value;
    }
}
