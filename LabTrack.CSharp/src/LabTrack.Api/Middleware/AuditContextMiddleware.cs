using System.Security.Claims;

namespace LabTrack.Api.Middleware;

public interface IAuditContext
{
    Guid? UserId { get; }
    string? Username { get; }
    string TraceId { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }
}

public sealed class AuditContext : IAuditContext
{
    public Guid? UserId { get; set; }
    public string? Username { get; set; }
    public string TraceId { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

public sealed class AuditContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditContextMiddleware> _logger;

    public AuditContextMiddleware(RequestDelegate next, ILogger<AuditContextMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, AuditContext auditContext)
    {
        auditContext.TraceId = context.TraceIdentifier;
        auditContext.IpAddress = context.Connection.RemoteIpAddress?.ToString();
        auditContext.UserAgent = context.Request.Headers["User-Agent"].ToString();

        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                           ?? context.User.FindFirst("sub")?.Value;

            if (Guid.TryParse(userIdClaim, out var userId))
            {
                auditContext.UserId = userId;
            }

            auditContext.Username = context.User.FindFirst(ClaimTypes.Name)?.Value 
                                   ?? context.User.FindFirst("preferred_username")?.Value;
        }

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["TraceId"] = auditContext.TraceId,
            ["UserId"] = auditContext.UserId?.ToString() ?? "Anonymous",
            ["IpAddress"] = auditContext.IpAddress ?? "Unknown"
        }))
        {
            await _next(context);
        }
    }
}