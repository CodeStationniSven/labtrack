using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LabTrack.Api.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Distributed;

namespace LabTrack.Api.Filters;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class IdempotentAttribute : Attribute, IAsyncActionFilter
{
    private const string IdempotencyHeaderName = "X-Idempotency-Key";
    private readonly TimeSpan _cacheDuration;

    public IdempotentAttribute(int cacheDurationMinutes = 60)
    {
        _cacheDuration = TimeSpan.FromMinutes(cacheDurationMinutes);
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;

        if (!httpContext.Request.Headers.TryGetValue(IdempotencyHeaderName, out var headerValue) ||
            string.IsNullOrWhiteSpace(headerValue))
        {
            context.Result = new BadRequestObjectResult(ApiResponse<object>.Fail(
                "MISSING_IDEMPOTENCY_KEY",
                $"Requests to this endpoint require a valid '{IdempotencyHeaderName}' header."));
            return;
        }

        var idempotencyKey = headerValue.ToString();
        var cache = httpContext.RequestServices.GetRequiredService<IDistributedCache>();
        var logger = httpContext.RequestServices.GetRequiredService<ILogger<IdempotentAttribute>>();

        var cacheKey = $"idempotency:{httpContext.Request.Path}:{idempotencyKey}";

        try
        {
            var cachedData = await cache.GetAsync(cacheKey, httpContext.RequestAborted);
            if (cachedData is not null)
            {
                var cachedResponse = JsonSerializer.Deserialize<CachedIdempotentResponse>(cachedData);
                if (cachedResponse is not null)
                {
                    logger.LogInformation("Returning cached idempotent response for key {IdempotencyKey}", idempotencyKey);
                    
                    context.Result = new ObjectResult(cachedResponse.ResponseBody)
                    {
                        StatusCode = cachedResponse.StatusCode
                    };
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reading idempotency key {IdempotencyKey} from cache", idempotencyKey);
        }

        var executedContext = await next();

        if (executedContext.Result is ObjectResult executedObjectResult && executedObjectResult.StatusCode is >= 200 and < 300)
        {
            try
            {
                var responseToCache = new CachedIdempotentResponse(
                    executedObjectResult.StatusCode.Value,
                    executedObjectResult.Value);

                var serializedResponse = JsonSerializer.SerializeToUtf8Bytes(responseToCache);

                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _cacheDuration
                };

                await cache.SetAsync(cacheKey, serializedResponse, options, httpContext.RequestAborted);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error saving idempotent response to cache for key {IdempotencyKey}", idempotencyKey);
            }
        }
    }

    private sealed record CachedIdempotentResponse(
        int StatusCode,
        object? ResponseBody
    );
}