using System.Security.Cryptography;
using System.Text;
using LabTrack.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace LabTrack.Infrastructure.Qr;

public class QrTokenService : IQrTokenService
{
    private readonly IConfiguration _configuration;

    public QrTokenService(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public string GenerateToken(string payload, Guid sessionId)
    {
        if (string.IsNullOrWhiteSpace(payload))
            throw new ArgumentException("Payload cannot be null or empty.", nameof(payload));

        var secret = _configuration["Qr:SigningKey"]
            ?? throw new InvalidOperationException("Qr:SigningKey is not configured.");
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds();

        var encodedPayload = Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));
        var signedData = $"{encodedPayload}|{sessionId}|{expiresAt}";
        var signature = ComputeHmac(signedData, secret);
        var raw = $"{signedData}|{signature}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
    }

    public bool ValidateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        try
        {
            var raw = Encoding.UTF8.GetString(Convert.FromBase64String(token));
            var parts = raw.Split('|');
            if (parts.Length != 4) return false;

            if (!Guid.TryParse(parts[1], out _)) return false;
            if (!long.TryParse(parts[2], out var exp)) return false;

            var secret = _configuration["Qr:SigningKey"]
                ?? throw new InvalidOperationException("Qr:SigningKey is not configured.");

            var signedData = $"{parts[0]}|{parts[1]}|{parts[2]}";
            var expected = ComputeHmac(signedData, secret);

            if (!CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(expected),
                    Encoding.UTF8.GetBytes(parts[3])))
                return false;

            if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > exp) return false;

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string ComputeHmac(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToBase64String(hash);
    }
}