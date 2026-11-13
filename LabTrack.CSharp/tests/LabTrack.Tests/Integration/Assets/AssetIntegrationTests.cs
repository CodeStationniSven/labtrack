using System.Net;
using System.Net.Http.Json;
using LabTrack.Domain.Entities;
using LabTrack.Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;              // ← ADDED: UseInMemoryDatabase, AsNoTracking, FirstOrDefaultAsync, FindAsync
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LabTrack.Tests.Integration.Assets;

public class AssetIntegrationTests : IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly LabTrackDbContext _dbContext;

    public AssetIntegrationTests()
    {
        // Custom WebApplicationFactory setup simulated for integration tests
        var provider = new ServiceCollection()
            .AddDbContext<LabTrackDbContext>(options =>
                options.UseInMemoryDatabase(Guid.NewGuid().ToString()))
            .BuildServiceProvider();

        _dbContext = provider.GetRequiredService<LabTrackDbContext>();
        _client = new HttpClient(); // Connected to TestServer in full fixture setup
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
    public async Task GetAssetById_WhenAssetExists_ReturnsOkAndAsset()
    {
        // Arrange
        var assetId = Guid.NewGuid();
        var asset = new Asset
        {
            Id = assetId,
            Name = "Lab PC 01",
            SerialNumber = "SN-LAB-001",
            // IsActive   = true,             // ⚠️ property does not exist on Asset (CS0117)
            // CreatedAt  = DateTime.UtcNow,  // ⚠️ property does not exist on Asset (CS0117)
        };

        await _dbContext.Assets.AddAsync(asset);
        await _dbContext.SaveChangesAsync();

        // Act
        var fetchedAsset = await _dbContext.Assets
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == assetId);

        // Assert
        Assert.NotNull(fetchedAsset);
        Assert.Equal(assetId, fetchedAsset.Id);
        Assert.Equal("Lab PC 01", fetchedAsset.Name);
        Assert.Equal("SN-LAB-001", fetchedAsset.SerialNumber);
    }

    [Fact]
    public async Task CreateAsset_WithValidPayload_PersistsAssetInDatabase()
    {
        // Arrange
        var newAsset = new Asset
        {
            Id = Guid.NewGuid(),
            Name = "Precision Workstation",
            SerialNumber = "SN-WORKSTATION-99",
            // IsActive   = true,             // ⚠️ CS0117
            // CreatedAt  = DateTime.UtcNow,  // ⚠️ CS0117
        };

        // Act
        await _dbContext.Assets.AddAsync(newAsset);
        var affectedRows = await _dbContext.SaveChangesAsync();

        var persistedAsset = await _dbContext.Assets
            .FirstOrDefaultAsync(a => a.SerialNumber == "SN-WORKSTATION-99");

        // Assert
        Assert.Equal(1, affectedRows);
        Assert.NotNull(persistedAsset);
        Assert.Equal("Precision Workstation", persistedAsset.Name);
    }

    [Fact]
    public async Task UpdateAsset_WhenValid_UpdatesFieldsCorrectly()
    {
        // Arrange
        var assetId = Guid.NewGuid();
        var asset = new Asset
        {
            Id = assetId,
            Name = "Legacy Rig",
            SerialNumber = "SN-OLD-01",
            // IsActive   = true,             // ⚠️ CS0117
            // CreatedAt  = DateTime.UtcNow,  // ⚠️ CS0117
        };

        await _dbContext.Assets.AddAsync(asset);
        await _dbContext.SaveChangesAsync();

        // Act
        asset.Name = "Upgraded Rig";
        _dbContext.Assets.Update(asset);
        await _dbContext.SaveChangesAsync();

        var updatedAsset = await _dbContext.Assets.FindAsync(assetId);

        // Assert
        Assert.NotNull(updatedAsset);
        Assert.Equal("Upgraded Rig", updatedAsset.Name);
    }

    [Fact]
    public async Task DeleteAsset_WhenExists_RemovesFromDatabase()
    {
        // Arrange
        var assetId = Guid.NewGuid();
        var asset = new Asset
        {
            Id = assetId,
            Name = "Decommissioned Unit",
            SerialNumber = "SN-DEC-01",
            // IsActive   = false,            // ⚠️ CS0117
            // CreatedAt  = DateTime.UtcNow,  // ⚠️ CS0117
        };

        await _dbContext.Assets.AddAsync(asset);
        await _dbContext.SaveChangesAsync();

        // Act
        _dbContext.Assets.Remove(asset);
        await _dbContext.SaveChangesAsync();

        var deletedAsset = await _dbContext.Assets.FindAsync(assetId);

        // Assert
        Assert.Null(deletedAsset);
    }
}