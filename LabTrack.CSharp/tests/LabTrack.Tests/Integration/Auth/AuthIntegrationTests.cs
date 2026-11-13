using System.Net;
using LabTrack.Domain.Entities;
using LabTrack.Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;                    // ← ADDED: fixes Include, FirstOrDefaultAsync, AnyAsync
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LabTrack.Tests.Integration.Auth;

public class AuthIntegrationTests : IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly LabTrackDbContext _dbContext;

    public AuthIntegrationTests()
    {
        var provider = new ServiceCollection()
            .AddDbContext<LabTrackDbContext>(options =>
                options.UseInMemoryDatabase(Guid.NewGuid().ToString()))
            .BuildServiceProvider();

        _dbContext = provider.GetRequiredService<LabTrackDbContext>();
        _client = new HttpClient();
    }

    public async Task InitializeAsync()
    {
        await _dbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _dbContext.Database.EnsureDeletedAsync();
        await _dbContext.DisposeAsync();
        _client.Dispose();
    }

    [Fact]
    public async Task GetUserByEmail_WhenUserExists_ReturnsUserWithAuthSessions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "tech.lead@labtrack.io",
            FirstName = "Alex",
            LastName = "Mercer",
            IsActive = true,
            // CreatedAt = DateTime.UtcNow,             // ⚠️ CS0117 — wala sa User entity
            AuthSessions = new List<AuthSession>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    // TokenHash = "hash-token-12345",           // ⚠️ CS0117 — wala sa AuthSession
                    // ExpiresAt = DateTime.UtcNow.AddHours(24), // ⚠️ CS0117
                    // CreatedAt = DateTime.UtcNow              // ⚠️ CS0117
                }
            }
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        var fetchedUser = await _dbContext.Users
            .Include(u => u.AuthSessions)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email.ToLower() == "tech.lead@labtrack.io");

        // Assert
        Assert.NotNull(fetchedUser);
        Assert.Equal(userId, fetchedUser.Id);
        Assert.Equal("Alex", fetchedUser.FirstName);
        Assert.Single(fetchedUser.AuthSessions);
    }

    [Fact]
    public async Task CreateAuthSession_ForExistingUser_PersistsSessionCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "lab.tech@labtrack.io",
            FirstName = "Jordan",
            LastName = "Vance",
            IsActive = true,
            // CreatedAt = DateTime.UtcNow,             // ⚠️ CS0117
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        var session = new AuthSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            // TokenHash = "secure-jwt-hash-value",     // ⚠️ CS0117
            // ExpiresAt = DateTime.UtcNow.AddHours(8), // ⚠️ CS0117
            // CreatedAt = DateTime.UtcNow              // ⚠️ CS0117
        };

        // Act
        await _dbContext.AuthSessions.AddAsync(session);
        var affectedRows = await _dbContext.SaveChangesAsync();

        var activeSession = await _dbContext.AuthSessions
            .FirstOrDefaultAsync(s => s.UserId == userId);   // ← filter lang sa UserId (non-existent TokenHash removed)

        // Assert
        Assert.Equal(1, affectedRows);
        Assert.NotNull(activeSession);
        Assert.Equal(userId, activeSession.UserId);
    }

    [Fact]
    public async Task RevokeAuthSession_WhenValid_RemovesSessionFromDatabase()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var session = new AuthSession
        {
            Id = sessionId,
            UserId = userId,
            // TokenHash = "revocable-token-hash",      // ⚠️ CS0117
            // ExpiresAt = DateTime.UtcNow.AddHours(1), // ⚠️ CS0117
            // CreatedAt = DateTime.UtcNow              // ⚠️ CS0117
        };

        await _dbContext.AuthSessions.AddAsync(session);
        await _dbContext.SaveChangesAsync();

        // Act
        _dbContext.AuthSessions.Remove(session);
        await _dbContext.SaveChangesAsync();

        var deletedSession = await _dbContext.AuthSessions.FindAsync(sessionId);

        // Assert
        Assert.Null(deletedSession);
    }

    [Fact]
    public async Task IsEmailUnique_WhenEmailExists_ReturnsFalse()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "unique.check@labtrack.io",
            FirstName = "Sam",
            LastName = "Oak",
            IsActive = true,
            // CreatedAt = DateTime.UtcNow,             // ⚠️ CS0117
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        var isUnique = !await _dbContext.Users
            .AnyAsync(u => u.Email.ToLower() == "unique.check@labtrack.io");

        // Assert
        Assert.False(isUnique);
    }
}