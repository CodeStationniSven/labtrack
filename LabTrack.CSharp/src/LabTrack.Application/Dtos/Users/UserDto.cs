namespace LabTrack.Application.Dtos.Users;

public sealed record UserDto
{
    public Guid Id { get; init; }
    public string SchoolId { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string Email { get; init; } = string.Empty;
    public string Role { get; init; } = "Student";

    public IReadOnlyList<string> Roles
    {
        get => new[] { Role };
        init => Role = value?.FirstOrDefault() ?? "Student";
    }

    public Guid? AssignedLaboratoryId { get; init; }
    public string? AssignedLaboratoryName { get; init; }
    public string? Department { get; init; }

    public bool IsActive { get; init; } = true;
    public string SystemStatus { get; init; } = "Active";
    public DateTime? LastLoginAtUtc { get; init; }
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; init; }
    public string? ProfilePictureUrl { get; init; }

}