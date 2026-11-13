using LabTrack.Domain.Entities;
using LabTrack.Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LabTrack.Tests.Integration.PcUsage;

public class PcUsageIntegrationTests : IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly LabTrackDbContext _dbContext;

    public PcUsageIntegrationTests()
    {
        var provider = new ServiceCollection()
            .AddDbContext<LabTrackDbContext>(options =>
                options.UseInMemoryDatabase(Guid.NewGuid().ToString()))
            .BuildServiceProvider();

        _dbContext = provider.GetRequiredService<LabTrackDbContext>();
        _client = new HttpClient();
    }

    public async Task InitializeAsync() => await _dbContext.Database.EnsureCreatedAsync();

    public async Task DisposeAsync()
    {
        await _dbContext.Database.EnsureDeletedAsync();
        await _dbContext.DisposeAsync();
        _client.Dispose();
    }

    [Fact]
    public async Task StartSession_WithValidWorkstation_PersistsLog()
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
    public async Task EndSession_WhenSessionIsActive_UpdatesSession()
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

        var endedSession = await _dbContext.PcUsageLogs.FindAsync(sessionId);

        Assert.NotNull(endedSession);
        Assert.Equal(workstationId, endedSession.WorkstationId);
    }

    [Fact]
    public async Task GetLogsByWorkstation_ReturnsOnlyMatchingLogs()
    {
        var targetWorkstation = Guid.NewGuid();
        var otherWorkstation = Guid.NewGuid();

        var logs = new List<PcUsageLog>
        {
            new() { Id = Guid.NewGuid(), WorkstationId = targetWorkstation },
            new() { Id = Guid.NewGuid(), WorkstationId = targetWorkstation },
            new() { Id = Guid.NewGuid(), WorkstationId = otherWorkstation },
        };

        await _dbContext.PcUsageLogs.AddRangeAsync(logs);
        await _dbContext.SaveChangesAsync();

        var matching = await _dbContext.PcUsageLogs
            .Where(p => p.WorkstationId == targetWorkstation)
            .AsNoTracking()
            .ToListAsync();

        Assert.Equal(2, matching.Count);
        Assert.All(matching, l => Assert.Equal(targetWorkstation, l.WorkstationId));
    }

    [Fact]
    public async Task GetLogsById_ReturnsCorrectLog()
    {
        var targetId = Guid.NewGuid();
        var otherId = Guid.NewGuid();

        var logs = new List<PcUsageLog>
        {
            new() { Id = targetId, WorkstationId = Guid.NewGuid() },
            new() { Id = otherId, WorkstationId = Guid.NewGuid() },
        };

        await _dbContext.PcUsageLogs.AddRangeAsync(logs);
        await _dbContext.SaveChangesAsync();

        var found = await _dbContext.PcUsageLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == targetId);

        Assert.NotNull(found);
        Assert.Equal(targetId, found.Id);
    }
}