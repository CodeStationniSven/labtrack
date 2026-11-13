namespace LabTrack.Api.Middleware;

public sealed class RequestIdMiddleware
{
    private const string RequestIdHeaderName = "X-Request-ID";
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestIdMiddleware> _logger;

    public RequestIdMiddleware(RequestDelegate next, ILogger<RequestIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(RequestIdHeaderName, out var requestId) ||
            string.IsNullOrWhiteSpace(requestId))
        {
            requestId = Guid.NewGuid().ToString("N");
        }

        context.TraceIdentifier = requestId.ToString();

        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(RequestIdHeaderName))
            {
                context.Response.Headers.Append(RequestIdHeaderName, requestId.ToString());
            }

            return Task.CompletedTask;
        });

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["RequestId"] = requestId.ToString()
        }))
        {
            await _next(context);
        }
    }
}