using LabTrack.Domain.Entities;
using LabTrack.Infrastructure.Persistence.DbContext;
using System.Security.Claims;

namespace LabTrack.Api.Middleware;

public class AuditLogMiddleware
{
    private readonly RequestDelegate _next;
    public AuditLogMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext ctx, LabTrackDbContext db)
    {
        await _next(ctx);
        
        // Only log mutating methods + successful responses
        var method = ctx.Request.Method;
        if (method != "POST" && method != "PUT" && method != "PATCH" && method != "DELETE") return;
        if (ctx.Response.StatusCode >= 400) return;
        
        try
        {
            var path = ctx.Request.Path.Value ?? "";
            // Skip audit log's own writes to avoid recursion
            if (path.Contains("/audit-logs", StringComparison.OrdinalIgnoreCase)) return;
            
            Guid? userId = null;
            var sub = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(sub, out var uid)) userId = uid;
            
            var entity = ExtractEntityName(path);
            var action = method switch
            {
                "POST" => "CREATE",
                "PUT" => "UPDATE",
                "PATCH" => "PATCH",
                "DELETE" => "DELETE",
                _ => method
            };
            
            var log = new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Action = action,
                EntityName = entity,
                EntityId = ExtractIdFromPath(path) ?? Guid.NewGuid().ToString(),
                IpAddress = ctx.Connection.RemoteIpAddress?.ToString(),
                TimestampUtc = DateTime.UtcNow,
                UserAgent = ctx.Request.Headers["User-Agent"].ToString()
            };
            db.AuditLogs.Add(log);
            await db.SaveChangesAsync();
        }
        catch
        {
            // Silent fail — audit should not break request
        }
    }
    
    private static string ExtractEntityName(string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        // /api/v1/{entity}/...
        if (segments.Length >= 3) return segments[2];
        return "unknown";
    }
    
    private static string? ExtractIdFromPath(string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < segments.Length; i++)
        {
            if (Guid.TryParse(segments[i], out _)) return segments[i];
        }
        return null;
    }
}


