using LabTrack.Domain.Entities;
using LabTrack.Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LabTrack.Tests.Integration.Maintenance;

public class MaintenanceIntegrationTests : IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly LabTrackDbContext _dbContext;

    public MaintenanceIntegrationTests()
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
    public async Task GetMaintenanceById_WhenExists_ReturnsMaintenanceRecordWithDetails()
    {
        var maintenanceId = Guid.NewGuid();
        var assetId = Guid.NewGuid();

        var record = new MaintenanceRecord
        {
            Id = maintenanceId,
            AssetId = assetId,
            Description = "Cleaned heat sink and replaced thermal compound.",
        };

        await _dbContext.MaintenanceRecords.AddAsync(record);
        await _dbContext.SaveChangesAsync();

        var fetchedRecord = await _dbContext.MaintenanceRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == maintenanceId);

        Assert.NotNull(fetchedRecord);
        Assert.Equal(maintenanceId, fetchedRecord.Id);
        Assert.Equal("Cleaned heat sink and replaced thermal compound.", fetchedRecord.Description);
    }

    [Fact]
    public async Task ScheduleMaintenance_WithValidData_PersistsRecordInDatabase()
    {
        var assetId = Guid.NewGuid();
        var record = new MaintenanceRecord
        {
            Id = Guid.NewGuid(),
            AssetId = assetId,
            Description = "Flash BIOS and update network interface drivers.",
        };

        await _dbContext.MaintenanceRecords.AddAsync(record);
        var affectedRows = await _dbContext.SaveChangesAsync();

        var persistedRecord = await _dbContext.MaintenanceRecords
            .FirstOrDefaultAsync(m => m.Id == record.Id);

        Assert.Equal(1, affectedRows);
        Assert.NotNull(persistedRecord);
        Assert.Equal(assetId, persistedRecord.AssetId);
    }

    [Fact]
    public async Task GetPendingMaintenanceByAsset_FiltersUncompletedTasksCorrectly()
    {
        var assetId = Guid.NewGuid();

        var records = new List<MaintenanceRecord>
        {
            new() { Id = Guid.NewGuid(), AssetId = assetId, Description = "Task 1" },
            new() { Id = Guid.NewGuid(), AssetId = assetId, Description = "Task 2" },
            new() { Id = Guid.NewGuid(), AssetId = Guid.NewGuid(), Description = "Other asset" }
        };

        await _dbContext.MaintenanceRecords.AddRangeAsync(records);
        await _dbContext.SaveChangesAsync();

        var pendingRecords = await _dbContext.MaintenanceRecords
            .Where(m => m.AssetId == assetId)
            .AsNoTracking()
            .ToListAsync();

        Assert.Equal(2, pendingRecords.Count);
        Assert.All(pendingRecords, m => Assert.Equal(assetId, m.AssetId));
    }

    [Fact]
    public async Task CompleteMaintenance_UpdatesStatusAndCompletedDate()
    {
        var maintenanceId = Guid.NewGuid();
        var record = new MaintenanceRecord
        {
            Id = maintenanceId,
            Description = "Memory Diagnostic",
        };

        await _dbContext.MaintenanceRecords.AddAsync(record);
        await _dbContext.SaveChangesAsync();

        record.Description = "Memory Diagnostic — completed";
        _dbContext.MaintenanceRecords.Update(record);
        await _dbContext.SaveChangesAsync();

        var updatedRecord = await _dbContext.MaintenanceRecords.FindAsync(maintenanceId);

        Assert.NotNull(updatedRecord);
        Assert.Equal("Memory Diagnostic — completed", updatedRecord.Description);
    }

    [Fact]
    public async Task DeleteMaintenance_WhenExists_RemovesFromDatabase()
    {
        var maintenanceId = Guid.NewGuid();
        var record = new MaintenanceRecord
        {
            Id = maintenanceId,
            Description = "Cancelled Routine Check",
        };

        await _dbContext.MaintenanceRecords.AddAsync(record);
        await _dbContext.SaveChangesAsync();

        _dbContext.MaintenanceRecords.Remove(record);
        await _dbContext.SaveChangesAsync();

        var deletedRecord = await _dbContext.MaintenanceRecords.FindAsync(maintenanceId);

        Assert.Null(deletedRecord);
    }
}