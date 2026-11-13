using System.Collections.Concurrent;
using System.Net;
using Xunit;

namespace LabTrack.Tests.Security;

public class RateLimitTests
{
    private class SlidingWindowRateLimiter
    {
        private readonly int _maxRequests;
        private readonly TimeSpan _window;
        private readonly ConcurrentDictionary<string, ConcurrentQueue<DateTime>> _requestTracker = new();

        public SlidingWindowRateLimiter(int maxRequests, TimeSpan window)
        {
            _maxRequests = maxRequests;
            _window = window;
        }

        public bool IsRequestAllowed(string clientIp)
        {
            var now = DateTime.UtcNow;
            var windowStart = now - _window;

            var timestamps = _requestTracker.GetOrAdd(clientIp, _ => new ConcurrentQueue<DateTime>());

            // Purge expired timestamps outside the sliding window
            while (timestamps.TryPeek(out var oldest) && oldest < windowStart)
            {
                timestamps.TryDequeue(out _);
            }

            if (timestamps.Count >= _maxRequests)
            {
                return false;
            }

            timestamps.Enqueue(now);
            return true;
        }
    }

    [Fact]
    public void IsRequestAllowed_UnderLimit_ReturnsTrue()
    {
        // Arrange
        var limiter = new SlidingWindowRateLimiter(maxRequests: 5, window: TimeSpan.FromMinutes(1));
        var clientIp = "192.168.1.100";

        // Act & Assert
        for (int i = 0; i < 5; i++)
        {
            Assert.True(limiter.IsRequestAllowed(clientIp), $"Request {i + 1} should be allowed.");
        }
    }

    [Fact]
    public void IsRequestAllowed_ExceedingLimit_ReturnsFalseAndThrottles()
    {
        // Arrange
        var limiter = new SlidingWindowRateLimiter(maxRequests: 3, window: TimeSpan.FromMinutes(1));
        var clientIp = "10.0.0.50";

        // Act
        var req1 = limiter.IsRequestAllowed(clientIp);
        var req2 = limiter.IsRequestAllowed(clientIp);
        var req3 = limiter.IsRequestAllowed(clientIp);
        var req4 = limiter.IsRequestAllowed(clientIp); // 4th request exceeds threshold of 3

        // Assert
        Assert.True(req1);
        Assert.True(req2);
        Assert.True(req3);
        Assert.False(req4);
    }

    [Fact]
    public void IsRequestAllowed_TrackedSeparatelyPerClientIp()
    {
        // Arrange
        var limiter = new SlidingWindowRateLimiter(maxRequests: 2, window: TimeSpan.FromMinutes(1));
        var clientA = "172.16.0.1";
        var clientB = "172.16.0.2";

        // Act
        limiter.IsRequestAllowed(clientA);
        limiter.IsRequestAllowed(clientA);
        var clientABlocked = limiter.IsRequestAllowed(clientA);

        var clientAAllowed = limiter.IsRequestAllowed(clientB);

        // Assert
        Assert.False(clientABlocked);
        Assert.True(clientAAllowed);
    }

    [Fact]
    public async Task IsRequestAllowed_AfterWindowExpires_ResetsQuota()
    {
        // Arrange
        var limiter = new SlidingWindowRateLimiter(maxRequests: 2, window: TimeSpan.FromMilliseconds(100));
        var clientIp = "127.0.0.1";

        // Fill capacity
        limiter.IsRequestAllowed(clientIp);
        limiter.IsRequestAllowed(clientIp);
        Assert.False(limiter.IsRequestAllowed(clientIp));

        // Act - Wait for sliding window to elapse
        await Task.Delay(150);

        // Assert
        Assert.True(limiter.IsRequestAllowed(clientIp));
    }
}