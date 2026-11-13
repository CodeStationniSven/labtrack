using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace LabTrack.Tests.Security;

public class JwtTests
{
    private const string SecretKey = "SuperSecretKeyForJwtTokenGenerationLabTrack2026!";
    private const string Issuer = "https://labtrack.io";
    private const string Audience = "https://labtrack.io";

    [Fact]
    public void GenerateToken_WithValidClaims_ReturnsValidTokenString()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "user@labtrack.io";
        var role = "Technician";

        // Act
        var token = GenerateJwtToken(userId, email, role, TimeSpan.FromHours(1));

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);

        var handler = new JwtSecurityTokenHandler();
        Assert.True(handler.CanReadToken(token));
    }

    [Fact]
    public void ValidateToken_WithValidToken_ReturnsPrincipalWithCorrectClaims()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "admin@labtrack.io";
        var role = "Administrator";
        var token = GenerateJwtToken(userId, email, role, TimeSpan.FromHours(1));

        // Act
        var principal = ValidateJwtToken(token);

        // Assert
        Assert.NotNull(principal);
        Assert.Equal(userId.ToString(), principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        Assert.Equal(email, principal.FindFirst(ClaimTypes.Email)?.Value);
        Assert.Equal(role, principal.FindFirst(ClaimTypes.Role)?.Value);
    }

    [Fact]
    public void ValidateToken_WhenExpired_ThrowsSecurityTokenExpiredException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "expired@labtrack.io";
        var role = "Student";
        var token = GenerateJwtToken(userId, email, role, TimeSpan.FromSeconds(-10)); // Expired token

        // Act & Assert
        Assert.Throws<SecurityTokenExpiredException>(() => ValidateJwtToken(token));
    }

    [Fact]
    public void ValidateToken_WithTamperedSignature_ThrowsSecurityTokenInvalidSignatureException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "tampered@labtrack.io";
        var role = "Student";
        var validToken = GenerateJwtToken(userId, email, role, TimeSpan.FromHours(1));

        var parts = validToken.Split('.');
        var tamperedSignature = parts[2] + "tamper";
        var tamperedToken = $"{parts[0]}.{parts[1]}.{tamperedSignature}";

        // Act & Assert
        Assert.Throws<SecurityTokenInvalidSignatureException>(() => ValidateJwtToken(tamperedToken));
    }

    private static string GenerateJwtToken(Guid userId, string email, string role, TimeSpan expiryWindow)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(expiryWindow),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static ClaimsPrincipal ValidateJwtToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(SecretKey);

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = Issuer,
            ValidateAudience = true,
            ValidAudience = Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        return tokenHandler.ValidateToken(token, validationParameters, out _);
    }
}