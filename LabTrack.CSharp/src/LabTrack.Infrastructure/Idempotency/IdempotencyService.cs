using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace LabTrack.Infrastructure.Idempotency;

public interface IIdempotencyService
{
    Task<bool> IsRequestProcessedAsync(string key, CancellationToken cancellationToken = default);
    Task<TResponse?> GetProcessedResponseAsync<TResponse>(string key, CancellationToken cancellationToken = default);
    Task SaveResponseAsync<TResponse>(string key, TResponse response, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
}

public class IdempotencyRecord
{
    public string Key { get; set; } = string.Empty;
    public string ResponseJson { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class IdempotencyService : IIdempotencyService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<IdempotencyService> _logger;
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromHours(24);

    public IdempotencyService(
        IDistributedCache cache,
        ILogger<IdempotencyService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<bool> IsRequestProcessedAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        var cacheKey = GetCacheKey(key);
        var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);

        return !string.IsNullOrEmpty(cachedData);
    }

    public async Task<TResponse?> GetProcessedResponseAsync<TResponse>(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return default;
        }

        var cacheKey = GetCacheKey(key);
        var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);

        if (string.IsNullOrEmpty(cachedData))
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<TResponse>(cachedData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize cached idempotency response for key: {IdempotencyKey}", key);
            return default;
        }
    }

    public async Task SaveResponseAsync<TResponse>(
        string key,
        TResponse response,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key) || response is null)
        {
            return;
        }

        var cacheKey = GetCacheKey(key);
        var serializedData = JsonSerializer.Serialize(response);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? DefaultExpiration
        };

        await _cache.SetStringAsync(cacheKey, serializedData, options, cancellationToken);
        _logger.LogInformation("Saved idempotency result for key: {IdempotencyKey}", key);
    }

    private static string GetCacheKey(string key) => $"idempotency:{key.Trim()}";
}