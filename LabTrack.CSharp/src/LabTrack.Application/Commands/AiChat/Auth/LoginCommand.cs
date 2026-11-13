using MediatR;

namespace LabTrack.Application.Commands.AiChat.Auth;

public sealed record LoginCommand : IRequest<string>
{
    public string UsernameOrEmail { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
