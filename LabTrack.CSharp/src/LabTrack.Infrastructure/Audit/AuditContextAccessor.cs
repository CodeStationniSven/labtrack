using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace LabTrack.Infrastructure.Audit;

public interface IAuditContextAccessor
{
    string? UserId { get; }
    string? UserName { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }
    string? CorrelationId { get; }
}

public class AuditContextAccessor : IAuditContextAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditContextAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            return httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? httpContext.User.FindFirst("sub")?.Value;
        }
    }

    public string? UserName
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            return httpContext.User.FindFirst(ClaimTypes.Name)?.Value
                ?? httpContext.User.FindFirst("preferred_username")?.Value
                ?? httpContext.User.FindFirst(ClaimTypes.Email)?.Value;
        }
    }

    public string? IpAddress
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext is null) return null;

            if (httpContext.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor) &&
                !string.IsNullOrWhiteSpace(forwardedFor))
            {
                return forwardedFor.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries)[0].Trim();
            }

            return httpContext.Connection.RemoteIpAddress?.ToString();
        }
    }

    public string? UserAgent
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext is null) return null;

            return httpContext.Request.Headers.UserAgent.ToString();
        }
    }

    public string? CorrelationId
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext is null) return null;

            if (httpContext.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId) &&
                !string.IsNullOrWhiteSpace(correlationId))
            {
                return correlationId.ToString();
            }

            return httpContext.TraceIdentifier;
        }
    }
}