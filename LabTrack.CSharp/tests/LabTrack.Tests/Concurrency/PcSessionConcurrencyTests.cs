using System.Collections.Concurrent;
using System.Linq;
using Xunit;

namespace LabTrack.Tests.Concurrency;

public class PcSessionConcurrencyTests
{
    private readonly ConcurrentDictionary<Guid, Guid> _activeAssetSessions = new();

    [Fact]
    public async Task ConcurrentSessionStart_ForSameAsset_ShouldAllowOnlyOneSession()
    {
        // Arrange
        var assetId = Guid.NewGuid();
        var userIds = Enumerable.Range(0, 10).Select(_ => Guid.NewGuid()).ToList();
        int successfulStarts = 0;
        int failedStarts = 0;

        Task<bool> TryStartSessionAsync(Guid userId)
        {
            return Task.Run(() =>
            {
                // Attempt to register an active session for the target asset atomically
                if (_activeAssetSessions.TryAdd(assetId, userId))
                {
                    Interlocked.Increment(ref successfulStarts);
                    return true;
                }

                Interlocked.Increment(ref failedStarts);
                return false;
            });
        }

        // Act
        var tasks = userIds.Select(TryStartSessionAsync).ToArray();
        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(1, successfulStarts);
        Assert.Equal(9, failedStarts);
        Assert.Single(_activeAssetSessions);
        Assert.True(_activeAssetSessions.ContainsKey(assetId));
    }

    [Fact]
    public async Task ConcurrentSessionStart_ForDifferentAssets_ShouldAllSucceed()
    {
        // Arrange
        var assetCount = 10;
        var assetIds = Enumerable.Range(0, assetCount).Select(_ => Guid.NewGuid()).ToList();
        var userId = Guid.NewGuid();

        Task<bool> TryStartSessionAsync(Guid assetId)
        {
            return Task.Run(() =>
            {
                return _activeAssetSessions.TryAdd(assetId, userId);
            });
        }

        // Act
        var tasks = assetIds.Select(TryStartSessionAsync).ToArray();
        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(assetCount, results.Count(r => r));
        Assert.Equal(assetCount, _activeAssetSessions.Count);
    }

    [Fact]
    public async Task ConcurrentSessionEnd_ShouldBeIdempotentAndThreadSafe()
    {
        // Arrange
        var assetId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _activeAssetSessions.TryAdd(assetId, userId);

        int endAttemptsSucceeded = 0;

        Task<bool> TryEndSessionAsync()
        {
            return Task.Run(() =>
            {
                if (_activeAssetSessions.TryRemove(assetId, out _))
                {
                    Interlocked.Increment(ref endAttemptsSucceeded);
                    return true;
                }

                return false;
            });
        }

        // Act
        var tasks = Enumerable.Range(0, 5).Select(_ => TryEndSessionAsync()).ToArray();
        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(1, endAttemptsSucceeded);
        Assert.Empty(_activeAssetSessions);
    }
}