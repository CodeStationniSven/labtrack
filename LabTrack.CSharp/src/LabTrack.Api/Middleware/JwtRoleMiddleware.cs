using System.Security.Claims;

namespace LabTrack.Api.Middleware;

public sealed class JwtRoleMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<JwtRoleMiddleware> _logger;

    public JwtRoleMiddleware(RequestDelegate next, ILogger<JwtRoleMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // I-bypass ang 401 para sa AI Chat
        if (context.Request.Path.StartsWithSegments("/api/v1/ai/chat")) 
        {
            await _next(context);
            return;
        }
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var identity = (ClaimsIdentity)context.User.Identity;

            // Map custom or legacy JWT role claim types to standard ClaimTypes.Role if needed
            var customRoleClaims = identity.FindAll("role")
                .Concat(identity.FindAll("roles"))
                .Concat(identity.FindAll("user_role"))
                .ToList();

            foreach (var roleClaim in customRoleClaims)
            {
                if (!identity.HasClaim(c => c.Type == ClaimTypes.Role && c.Value == roleClaim.Value))
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, roleClaim.Value));
                }
            }

            // â”€â”€â”€ ROLE ALIASES (fix 403 role mismatch) â”€â”€â”€
            var roleAliases = new System.Collections.Generic.Dictionary<string, string[]>(System.StringComparer.OrdinalIgnoreCase)
            {
                ["ADMIN"] = new[] { "ADMIN_TECHNICIAN", "SUPER_ADMIN", "SUPERADMIN", "SYSADMIN", "ADMINISTRATOR" },
                ["TECHNICIAN"] = new[] { "ADMIN_TECHNICIAN", "TECH", "MAINTENANCE", "TECH_SUPPORT" },
                ["INSTRUCTOR"] = new[] { "TEACHER", "FACULTY", "LECTURER", "PROFESSOR" },
                ["STUDENT"] = new[] { "LEARNER", "USER", "PUPIL" }
            };
            var currentRolesList = identity.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            foreach (var currentRole in currentRolesList)
            {
                if (roleAliases.TryGetValue(currentRole, out var aliases))
                {
                    foreach (var alias in aliases)
                    {
                        if (!identity.HasClaim(c => c.Type == ClaimTypes.Role && c.Value == alias))
                        {
                            identity.AddClaim(new Claim(ClaimTypes.Role, alias));
                        }
                    }
                }
            }
            // â”€â”€â”€ END ROLE ALIASES â”€â”€â”€
                        // ─── ADMIN = TECHNICIAN (full control) ───
            // ADMIN gets all technician privileges so they can access
            // every endpoint that requires TECHNICIAN role
            var currentRoles = identity.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            var isAdmin = currentRoles.Any(r => 
                r.Equals("ADMIN", StringComparison.OrdinalIgnoreCase) ||
                r.Equals("ADMINISTRATOR", StringComparison.OrdinalIgnoreCase) ||
                r.Equals("SUPER_ADMIN", StringComparison.OrdinalIgnoreCase) ||
                r.Equals("LAB_ADMIN", StringComparison.OrdinalIgnoreCase));
            
            if (isAdmin)
            {
                foreach (var alias in new[] { "TECHNICIAN", "ADMIN_TECHNICIAN", "SUPER_ADMIN", "LAB_ADMIN" })
                {
                    if (!identity.HasClaim(c => c.Type == ClaimTypes.Role && c.Value == alias))
                    {
                        identity.AddClaim(new Claim(ClaimTypes.Role, alias));
                    }
                }
            }
            // ─── END ADMIN ALIASES ───
            var roles = identity.FindAll(ClaimTypes.Role).Select(c => c.Value);
            _logger.LogTrace(
                "User {UserId} authenticated with roles: {Roles}",
                identity.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown",
                string.Join(", ", roles));
        }

        await _next(context);
    }
}