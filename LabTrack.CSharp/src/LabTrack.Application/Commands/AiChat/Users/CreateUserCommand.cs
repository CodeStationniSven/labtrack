using MediatR;

namespace LabTrack.Application.Commands.AiChat.Users;

public sealed record CreateUserCommand : IRequest<Guid>
{
    public string SchoolId { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Role { get; init; } = "Student";
    public Guid? AssignedLaboratoryId { get; init; }
    public string? Department { get; init; }
    public string InitialPassword { get; init; } = string.Empty;

    public string Username
    {
        get => SchoolId;
        init => SchoolId = value;
    }

    public string Password
    {
        get => InitialPassword;
        init => InitialPassword = value;
    }

    public IReadOnlyList<string> Roles
    {
        get => new[] { Role };
        init => Role = value?.FirstOrDefault() ?? "Student";
    }
}
