using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LabTrack.Application.Common;

public interface IQrTokenService
{
    string GenerateToken(QrPayload payload, TimeSpan timeToLive);
    bool TryValidateToken(string token, out QrPayload? payload);
}

public sealed record QrPayload(
    Guid ResourceId,
    string TokenType, // e.g., "WORKSTATION_SCAN", "ASSET_CHECKIN"
    Guid GeneratedByUserId,
    DateTime ExpiresAtUtc
);

public sealed class QrTokenService : IQrTokenService
{
    private readonly byte[] _secretKey;

    public QrTokenService(string secret)
    {
        if (string.IsNullOrWhiteSpace(secret) || secret.Length < 16)
        {
            throw new ArgumentException("QR Secret key must be at least 16 characters long.", nameof(secret));
        }

        _secretKey = Encoding.UTF8.GetBytes(secret);
    }

    public string GenerateToken(QrPayload payload, TimeSpan timeToLive)
    {
        ArgumentNullException.ThrowIfNull(payload);

        var updatedPayload = payload with { ExpiresAtUtc = DateTime.UtcNow.Add(timeToLive) };
        var jsonPayload = JsonSerializer.Serialize(updatedPayload);
        var payloadBytes = Encoding.UTF8.GetBytes(jsonPayload);

        using var hmac = new HMACSHA256(_secretKey);
        var signatureBytes = hmac.ComputeHash(payloadBytes);

        var encodedPayload = Base64UrlEncode(payloadBytes);
        var encodedSignature = Base64UrlEncode(signatureBytes);

        return $"{encodedPayload}.{encodedSignature}";
    }

    public bool TryValidateToken(string token, out QrPayload? payload)
    {
        payload = null;

        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var parts = token.Split('.');
        if (parts.Length != 2)
        {
            return false;
        }

        try
        {
            var payloadBytes = Base64UrlDecode(parts[0]);
            var signatureBytes = Base64UrlDecode(parts[1]);

            using var hmac = new HMACSHA256(_secretKey);
            var computedSignature = hmac.ComputeHash(payloadBytes);

            if (!CryptographicOperations.FixedTimeEquals(computedSignature, signatureBytes))
            {
                return false;
            }

            var jsonPayload = Encoding.UTF8.GetString(payloadBytes);
            var parsedPayload = JsonSerializer.Deserialize<QrPayload>(jsonPayload);

            if (parsedPayload is null || parsedPayload.ExpiresAtUtc < DateTime.UtcNow)
            {
                return false;
            }

            payload = parsedPayload;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string Base64UrlEncode(byte[] input)
    {
        return Convert.ToBase64String(input)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var output = input
            .Replace('-', '+')
            .Replace('_', '/');

        switch (output.Length % 4)
        {
            case 2: output += "=="; break;
            case 3: output += "="; break;
        }

        return Convert.FromBase64String(output);
    }
}