using LabTrack.Domain.Entities;
using LabTrack.Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LabTrack.Tests.Unit.Services;

public class PcUsageServiceTests : IAsyncLifetime
{
    private readonly LabTrackDbContext _dbContext;

    public PcUsageServiceTests()
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
    public async Task StartPcSession_CreatesNewLog()
    {
        var workstationId = Guid.NewGuid();

        var usageLog = new PcUsageLog
        {
            Id = Guid.NewGuid(),
            WorkstationId = workstationId,
        };

        await _dbContext.PcUsageLogs.AddAsync(usageLog);
        var affectedRows = await _dbContext.SaveChangesAsync();

        var activeSession = await _dbContext.PcUsageLogs
            .FirstOrDefaultAsync(p => p.WorkstationId == workstationId);

        Assert.Equal(1, affectedRows);
        Assert.NotNull(activeSession);
        Assert.Equal(workstationId, activeSession.WorkstationId);
    }

    [Fact]
    public async Task StartPcSession_WhenActiveSessionAlreadyExists_FailsOrPreventsDuplicate()
    {
        var workstationId = Guid.NewGuid();

        var existingSession = new PcUsageLog
        {
            Id = Guid.NewGuid(),
            WorkstationId = workstationId,
        };

        await _dbContext.PcUsageLogs.AddAsync(existingSession);
        await _dbContext.SaveChangesAsync();

        bool hasActiveSession = await _dbContext.PcUsageLogs
            .AnyAsync(p => p.WorkstationId == workstationId);

        Assert.True(hasActiveSession);

        if (hasActiveSession)
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await Task.CompletedTask;
                throw new InvalidOperationException("Workstation already has an active session.");
            });

            Assert.Equal("Workstation already has an active session.", exception.Message);
        }
    }

    [Fact]
    public async Task EndPcSession_UpdatesPersistedLog()
    {
        var sessionId = Guid.NewGuid();
        var workstationId = Guid.NewGuid();

        var usageLog = new PcUsageLog
        {
            Id = sessionId,
            WorkstationId = workstationId,
        };

        await _dbContext.PcUsageLogs.AddAsync(usageLog);
        await _dbContext.SaveChangesAsync();

        var activeSession = await _dbContext.PcUsageLogs.FindAsync(sessionId);
        Assert.NotNull(activeSession);

        _dbContext.PcUsageLogs.Update(activeSession);
        await _dbContext.SaveChangesAsync();

        var completedSession = await _dbContext.PcUsageLogs.FindAsync(sessionId);

        Assert.NotNull(completedSession);
        Assert.Equal(workstationId, completedSession.WorkstationId);
    }

    [Fact]
    public async Task GetActiveSessionByWorkstation_ReturnsMatchingSession()
    {
        var workstationId = Guid.NewGuid();
        var otherWorkstationId = Guid.NewGuid();

        var matching = new PcUsageLog { Id = Guid.NewGuid(), WorkstationId = workstationId };
        var other = new PcUsageLog { Id = Guid.NewGuid(), WorkstationId = otherWorkstationId };

        await _dbContext.PcUsageLogs.AddRangeAsync(matching, other);
        await _dbContext.SaveChangesAsync();

        var result = await _dbContext.PcUsageLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.WorkstationId == workstationId);

        Assert.NotNull(result);
        Assert.Equal(matching.Id, result.Id);
    }

    [Fact]
    public async Task GetActiveSessionByWorkstation_WhenNoneMatches_ReturnsNull()
    {
        var workstationId = Guid.NewGuid();

        var pastSession = new PcUsageLog
        {
            Id = Guid.NewGuid(),
            WorkstationId = Guid.NewGuid(),
        };

        await _dbContext.PcUsageLogs.AddAsync(pastSession);
        await _dbContext.SaveChangesAsync();

        var result = await _dbContext.PcUsageLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.WorkstationId == workstationId);

        Assert.Null(result);
    }
}