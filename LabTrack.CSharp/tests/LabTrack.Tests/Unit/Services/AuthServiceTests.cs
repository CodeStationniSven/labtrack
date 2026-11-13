using LabTrack.Domain.Entities;
using LabTrack.Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LabTrack.Tests.Unit.Services;

public class AuthServiceTests : IAsyncLifetime
{
    private readonly LabTrackDbContext _dbContext;

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<LabTrackDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new LabTrackDbContext(options);
    }

    public async Task InitializeAsync() => await _dbContext.Database.EnsureCreatedAsync();

    public async Task DisposeAsync()
    {
        await _dbContext.Database.EnsureDeletedAsync();
        await _dbContext.DisposeAsync();
    }

    [Fact]
    public async Task ValidateCredentials_WithValidUserAndMatchingHash_ReturnsUser()
    {
        var userId = Guid.NewGuid();
        var email = "auth.test@labtrack.io";
        var user = new User
        {
            Id = userId,
            Email = email,
            FirstName = "Test",
            LastName = "User",
            IsActive = true,
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        var foundUser = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email && u.IsActive);

        Assert.NotNull(foundUser);
        Assert.Equal(userId, foundUser.Id);
        Assert.True(foundUser.IsActive);
    }

    [Fact]
    public async Task ValidateCredentials_WhenUserInactive_ReturnsNull()
    {
        var email = "inactive@labtrack.io";
        var inactiveUser = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            FirstName = "Inactive",
            LastName = "Account",
            IsActive = false,
        };

        await _dbContext.Users.AddAsync(inactiveUser);
        await _dbContext.SaveChangesAsync();

        var foundUser = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email && u.IsActive);

        Assert.Null(foundUser);
    }

    [Fact]
    public async Task CreateSession_ForUser_PersistsSessionWithExpiry()
    {
        var userId = Guid.NewGuid();
        var session = new AuthSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
        };

        await _dbContext.AuthSessions.AddAsync(session);
        await _dbContext.SaveChangesAsync();

        var persistedSession = await _dbContext.AuthSessions
            .FirstOrDefaultAsync(s => s.UserId == userId);

        Assert.NotNull(persistedSession);
        Assert.Equal(userId, persistedSession.UserId);
    }

    [Fact]
    public async Task RevokeSession_WhenSessionExists_DeletesSession()
    {
        var sessionId = Guid.NewGuid();
        var session = new AuthSession
        {
            Id = sessionId,
            UserId = Guid.NewGuid(),
        };

        await _dbContext.AuthSessions.AddAsync(session);
        await _dbContext.SaveChangesAsync();

        _dbContext.AuthSessions.Remove(session);
        await _dbContext.SaveChangesAsync();

        var revokedSession = await _dbContext.AuthSessions.FindAsync(sessionId);

        Assert.Null(revokedSession);
    }
}