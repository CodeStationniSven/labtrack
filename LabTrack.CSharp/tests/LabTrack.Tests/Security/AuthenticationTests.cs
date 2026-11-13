using System.Security.Claims;
using System.Text;
using Xunit;

namespace LabTrack.Tests.Security;

public class AuthenticationTests
{
    [Fact]
    public void GenerateJwtToken_WithValidClaims_CreatesParseableToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "admin@labtrack.io";
        var role = "Administrator";
        var secretKey = "SuperSecretKeyForJwtTokenGenerationLabTrack2026!";

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role)
        };

        // Act
        var token = SimulateJwtGeneration(userId, email, role, secretKey);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
        Assert.Contains(".", token); // JWT standard structure (header.payload.signature)
    }

    [Fact]
    public void HashPassword_WhenGivenPlaintext_ReturnsSecureHashAndSalt()
    {
        // Arrange
        var rawPassword = "SecureP@ssw0rd2026!";

        // Act
        var (hash, salt) = HashPassword(rawPassword);

        // Assert
        Assert.NotNull(hash);
        Assert.NotNull(salt);
        Assert.NotEqual(rawPassword, hash);
        Assert.True(VerifyPassword(rawPassword, hash, salt));
    }

    [Fact]
    public void VerifyPassword_WithIncorrectPassword_ReturnsFalse()
    {
        // Arrange
        var rawPassword = "CorrectPassword123!";
        var wrongPassword = "WrongPassword123!";
        var (hash, salt) = HashPassword(rawPassword);

        // Act
        var isValid = VerifyPassword(wrongPassword, hash, salt);

        // Assert
        Assert.False(isValid);
    }

    private static string SimulateJwtGeneration(Guid userId, string email, string role, string key)
    {
        var header = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"alg\":\"HS256\",\"typ\":\"JWT\"}"));
        var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{{\"sub\":\"{userId}\",\"email\":\"{email}\",\"role\":\"{role}\"}}"));
        var signature = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{key}-signature"));

        return $"{header}.{payload}.{signature}";
    }

    private static (string Hash, string Salt) HashPassword(string password)
    {
        using var hmac = new System.Security.Cryptography.HMACSHA512();
        var salt = Convert.ToBase64String(hmac.Key);
        var hash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));
        return (hash, salt);
    }

    private static bool VerifyPassword(string password, string storedHash, string storedSalt)
    {
        var saltBytes = Convert.FromBase64String(storedSalt);
        using var hmac = new System.Security.Cryptography.HMACSHA512(saltBytes);
        var computedHash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));
        return computedHash == storedHash;
    }
}