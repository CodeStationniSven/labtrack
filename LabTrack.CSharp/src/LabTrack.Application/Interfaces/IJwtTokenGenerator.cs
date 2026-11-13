using LabTrack.Domain.Entities;

namespace LabTrack.Application.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateAccessToken(
        User user, 
        IReadOnlyList<string> roles);

    string GenerateRefreshToken();

    Task<(string AccessToken, string RefreshToken)> GenerateTokensAsync(
        User user, 
        IReadOnlyList<string> roles, 
        CancellationToken cancellationToken = default);

    Task<Guid?> ValidateRefreshTokenAsync(
        string refreshToken, 
        CancellationToken cancellationToken = default);
}