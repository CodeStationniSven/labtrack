using LabTrack.Application.Dtos.Users;

namespace LabTrack.Application.Dtos.Auth;

public sealed record AuthResponseDto
{
    public Guid UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public DateTime AccessTokenExpiresAtUtc { get; init; }
    public DateTime RefreshTokenExpiresAtUtc { get; init; }

   
    public DateTime ExpiresAtUtc { get; init; }
    public UserDto? User { get; init; }
}