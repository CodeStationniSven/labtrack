using System.Collections.Concurrent;
using System.Net;
using Xunit;

namespace LabTrack.Tests.Concurrency;

public class IdempotencyTests
{
    private readonly ConcurrentDictionary<string, string> _processedRequests = new();

    [Fact]
    public async Task ConcurrentRequests_WithSameIdempotencyKey_ShouldExecuteOnlyOnce()
    {
        // Arrange
        const string idempotencyKey = "test-idempotency-key-12345";
        int executionCount = 0;
        var cancellationToken = CancellationToken.None;

        Task<string> ExecuteIdempotentActionAsync(string key)
        {
            return Task.Run(() =>
            {
                // Atomically check and add key
                if (_processedRequests.TryAdd(key, "PROCESSING"))
                {
                    Interlocked.Increment(ref executionCount);
                    // Simulate work
                    Thread.Sleep(50);
                    _processedRequests[key] = "PROCESSED_RESULT";
                    return "PROCESSED_RESULT";
                }

                // Wait for the original task to populate the result if key already exists
                while (_processedRequests[key] == "PROCESSING")
                {
                    Thread.Sleep(5);
                }

                return _processedRequests[key];
            });
        }

        // Act
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => ExecuteIdempotentActionAsync(idempotencyKey))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(1, executionCount);
        Assert.All(results, res => Assert.Equal("PROCESSED_RESULT", res));
    }

    [Fact]
    public async Task ConcurrentRequests_WithDifferentKeys_ShouldExecuteIndependently()
    {
        // Arrange
        int executionCount = 0;

        Task<string> ExecuteIdempotentActionAsync(string key)
        {
            return Task.Run(() =>
            {
                if (_processedRequests.TryAdd(key, "PROCESSING"))
                {
                    Interlocked.Increment(ref executionCount);
                    _processedRequests[key] = $"RESULT_FOR_{key}";
                    return $"RESULT_FOR_{key}";
                }

                return _processedRequests[key];
            });
        }

        // Act
        var tasks = Enumerable.Range(1, 5)
            .Select(i => ExecuteIdempotentActionAsync($"key-{i}"))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(5, executionCount);
        Assert.Equal(5, results.Distinct().Count());
    }
}