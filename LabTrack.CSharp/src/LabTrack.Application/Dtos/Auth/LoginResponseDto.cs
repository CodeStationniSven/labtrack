namespace LabTrack.Application.Dtos.Auth;

public sealed record LoginResponseDto
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public DateTime ExpiresAtUtc { get; init; }
    public UserDetailsDto User { get; init; } = null!;
}

public sealed record UserDetailsDto
{
    public Guid Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public IReadOnlyCollection<string> Roles { get; init; } = Array.Empty<string>();
    public Guid? AssignedLaboratoryId { get; init; }
}