using System.Security.Cryptography;
using LabTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LabTrack.Infrastructure.Authentication;

public interface IRefreshTokenService
{
    Task<RefreshToken> GenerateRefreshTokenAsync(Guid userId, string? ipAddress, CancellationToken cancellationToken = default);
    Task<bool> ValidateRefreshTokenAsync(string token, CancellationToken cancellationToken = default);
    Task RevokeRefreshTokenAsync(string token, string? ipAddress, string? reason = null, CancellationToken cancellationToken = default);
    Task RevokeAllUserTokensAsync(Guid userId, string? ipAddress, string? reason = null, CancellationToken cancellationToken = default);
}

public class RefreshTokenService : IRefreshTokenService
{
    private readonly DbContext _context;
    private const int DefaultExpiryDays = 7;

    public RefreshTokenService(DbContext context)
    {
        _context = context;
    }

    public async Task<RefreshToken> GenerateRefreshTokenAsync(Guid userId, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = Convert.ToBase64String(randomBytes),
            ExpiresAt = DateTime.UtcNow.AddDays(DefaultExpiryDays),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress
        };

        await _context.Set<RefreshToken>().AddAsync(refreshToken, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return refreshToken;
    }

    public async Task<bool> ValidateRefreshTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var refreshToken = await _context.Set<RefreshToken>()
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Token == token, cancellationToken);

        if (refreshToken is null)
        {
            return false;
        }

        return refreshToken.IsActive;
    }

    public async Task RevokeRefreshTokenAsync(string token, string? ipAddress, string? reason = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return;

        var refreshToken = await _context.Set<RefreshToken>()
            .FirstOrDefaultAsync(r => r.Token == token, cancellationToken);

        if (refreshToken is null || !refreshToken.IsActive)
        {
            return;
        }

        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.RevokedByIp = ipAddress;
        refreshToken.ReasonRevoked = reason ?? "Revoked manually";

        _context.Set<RefreshToken>().Update(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeAllUserTokensAsync(Guid userId, string? ipAddress, string? reason = null, CancellationToken cancellationToken = default)
    {
        var activeTokens = await _context.Set<RefreshToken>()
            .Where(r => r.UserId == userId && r.RevokedAt == null && r.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        if (activeTokens.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var token in activeTokens)
        {
            token.RevokedAt = now;
            token.RevokedByIp = ipAddress;
            token.ReasonRevoked = reason ?? "Bulk token revocation";
        }

        _context.Set<RefreshToken>().UpdateRange(activeTokens);
        await _context.SaveChangesAsync(cancellationToken);
    }
}