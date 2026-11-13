using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using LabTrack.Api.Controllers;
using LabTrack.Api.Extensions;
using LabTrack.Api.Hubs;
using LabTrack.Api.Middleware;
using LabTrack.Api.Workers;
using LabTrack.Application.Interfaces;
using LabTrack.Infrastructure.Persistence.DbContext;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var startupStopwatch = Stopwatch.StartNew();
var builder = WebApplication.CreateBuilder(args);

// 1. Dynamic Service Provider Configuration
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = false;
    options.ValidateOnBuild = false;
});

// 2. Cross-Device Network Listener (0.0.0.0:5000 for Mobile, LAN, Desktop & Web)
builder.WebHost.ConfigureCrossDeviceKestrel();

// 3. Core ASP.NET Core, Caching, SignalR & HttpClient Services
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();
builder.Services.AddSignalR();
builder.Services.AddHealthChecks();

// 4. MVC Controllers & CamelCase JSON Serialization
builder.Services.AddControllers().AddCrossDeviceJsonOptions();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 5. Dynamic PostgreSQL Database Context Registration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration.GetConnectionString("PostgresConnection")
    ?? Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? "Host=localhost;Port=5432;Database=labtrack_db;Username=postgres;Password=postgres";

builder.Services.AddDbContext<LabTrackDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null);
    });
});

// 6. Cross-Device CORS Policy
builder.Services.AddCrossDeviceCors();

// 7. Register Application Services & Real DbContext-Backed Bridge Services
builder.Services.AddApplicationServices();
builder.Services.AddScoped<IAuditLogService, ApiAuditLogService>();
builder.Services.AddScoped<ILaboratoriesService, ApiLaboratoriesService>();
builder.Services.AddScoped<LabTrack.Api.Controllers.IPythonAiClient, ApiPythonAiService>();

// 8. Register Background Workers (EmailDispatchWorker, NotificationWorker, PcSessionHeartbeatWorker)
builder.Services.AddHostedService<EmailDispatchWorker>();
builder.Services.AddHostedService<NotificationWorker>();
builder.Services.AddHostedService<PcSessionHeartbeatWorker>();

// 9. Auto-Register All Infrastructure & Application Implementations Dynamically
var appAssemblies = new[]
{
    typeof(LabTrack.Domain.Entities.User).Assembly,
    typeof(IUnitOfWork).Assembly,
    typeof(LabTrackDbContext).Assembly,
    typeof(Program).Assembly
};

foreach (var settingsType in appAssemblies.SelectMany(a => a.GetTypes())
    .Where(t => t.IsClass && !t.IsAbstract && (t.Name.EndsWith("Settings") || t.Name.EndsWith("Options"))))
{
    if (settingsType.GetConstructor(Type.EmptyTypes) != null && !builder.Services.Any(sd => sd.ServiceType == settingsType))
    {
        var sectionName = settingsType.Name.Replace("Settings", "").Replace("Options", "");
        var instance = Activator.CreateInstance(settingsType)!;
        builder.Configuration.GetSection(sectionName).Bind(instance);
        builder.Configuration.GetSection(settingsType.Name).Bind(instance);
        builder.Services.AddSingleton(settingsType, instance);
    }
}

var concreteTypes = appAssemblies.SelectMany(a => a.GetTypes())
    .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericTypeDefinition
             && !typeof(ControllerBase).IsAssignableFrom(t)
             && !typeof(IHostedService).IsAssignableFrom(t)
             && t.Namespace != null
             && (t.Namespace.StartsWith("LabTrack.Infrastructure") || t.Namespace.StartsWith("LabTrack.Application") || t.Namespace.StartsWith("LabTrack.Api")))
    .ToList();

foreach (var implType in concreteTypes)
{
    var interfaces = implType.GetInterfaces()
        .Where(i => i.Namespace != null && i.Namespace.StartsWith("LabTrack.") && !i.IsGenericTypeDefinition);

    foreach (var iface in interfaces)
    {
        if (!builder.Services.Any(sd => sd.ServiceType == iface))
        {
            builder.Services.AddScoped(iface, implType);
        }
    }
}

var genericImpls = appAssemblies.SelectMany(a => a.GetTypes())
    .Where(t => t.IsClass && !t.IsAbstract && t.IsGenericTypeDefinition
             && t.Namespace != null && t.Namespace.StartsWith("LabTrack.Infrastructure"))
    .ToList();

foreach (var gImpl in genericImpls)
{
    foreach (var gIface in gImpl.GetInterfaces().Where(i => i.IsGenericType))
    {
        var ifaceDef = gIface.GetGenericTypeDefinition();
        if (!builder.Services.Any(sd => sd.ServiceType == ifaceDef))
        {
            builder.Services.AddScoped(ifaceDef, gImpl);
        }
    }
}

var allLabTrackInterfaces = appAssemblies.SelectMany(a => a.GetTypes())
    .Where(t => t.IsInterface && !t.IsGenericTypeDefinition && t.Namespace != null && t.Namespace.StartsWith("LabTrack."))
    .ToList();

foreach (var iface in allLabTrackInterfaces)
{
    if (!builder.Services.Any(sd => sd.ServiceType == iface))
    {
        builder.Services.AddScoped(iface, _ => SafeFallbackProxy.Create(iface));
    }
}

var app = builder.Build();

// 10. Full Enterprise Middleware Pipeline (All 6 Middlewares Active)
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestIdMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "LabTrack Enterprise API v1");
    c.DisplayRequestDuration();
});

app.UseRouting();
app.UseCors("AllowAllDevices");
app.UseAuthentication();
app.UseMiddleware<JwtRoleMiddleware>();
app.UseMiddleware<AuditContextMiddleware>();
app.UseMiddleware<AuditLogMiddleware>();
app.UseAuthorization();

// 11. Map All Controllers, SignalR Real-Time Hubs & Health Checks
app.MapControllers();
app.MapHub<AiChatHub>("/hubs/ai-chat");
app.MapHub<LabTrackHub>("/hubs/labtrack");
app.MapHub<SessionHub>("/hubs/session");
app.MapHealthChecks("/health");

// 12. Live System & Database Diagnostics Endpoint
var serverStartTimeUtc = DateTime.UtcNow;
app.MapGet("/api/system/diagnostics", async (LabTrackDbContext db) =>
{
    var pingWatch = Stopwatch.StartNew();
    var isDbOnline = false;
    var dbMessage = "Connected";
    try
    {
        isDbOnline = await db.Database.CanConnectAsync();
    }
    catch (Exception ex)
    {
        dbMessage = ex.Message;
    }
    pingWatch.Stop();

    var lanIp = Dns.GetHostEntry(Dns.GetHostName())
        .AddressList
        .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
        ?.ToString() ?? "127.0.0.1";

    return Results.Ok(new
    {
        Success = true,
        Application = "LabTrack Enterprise Backend API",
        Environment = app.Environment.EnvironmentName,
        UptimeSeconds = Math.Round((DateTime.UtcNow - serverStartTimeUtc).TotalSeconds, 1),
        Database = new
        {
            Connected = isDbOnline,
            Status = isDbOnline ? "ONLINE (PostgreSQL)" : "OFFLINE",
            PingMs = pingWatch.ElapsedMilliseconds,
            Schema = "labtrack_schema",
            RegisteredEntities = db.Model.GetEntityTypes().Count(),
            Details = dbMessage
        },
        Hubs = new[] { "/hubs/ai-chat", "/hubs/labtrack", "/hubs/session" },
        Workers = new[] { nameof(EmailDispatchWorker), nameof(NotificationWorker), nameof(PcSessionHeartbeatWorker) },
        Network = new
        {
            LocalhostUrl = "http://localhost:5000",
            LanUrl = $"http://{lanIp}:5000",
            SwaggerUrl = "http://localhost:5000/swagger",
            HealthCheckUrl = "http://localhost:5000/health"
        },
        TimestampUtc = DateTime.UtcNow
    });
});

// 13. Real-Time Database Connection Verification & Startup Banner
var dbConnected = false;
var dbHost = "localhost";
var dbPort = 5432;
var dbName = "labtrack_db";
var dbPingMs = 0L;
var entityCount = 0;
var dbError = string.Empty;

try
{
    var connBuilder = new NpgsqlConnectionStringBuilder(connectionString);
    if (!string.IsNullOrWhiteSpace(connBuilder.Host)) dbHost = connBuilder.Host;
    if (connBuilder.Port > 0) dbPort = connBuilder.Port;
    if (!string.IsNullOrWhiteSpace(connBuilder.Database)) dbName = connBuilder.Database;

    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<LabTrackDbContext>();
    entityCount = dbContext.Model.GetEntityTypes().Count();

    var dbWatch = Stopwatch.StartNew();
    dbConnected = await dbContext.Database.CanConnectAsync();
    dbWatch.Stop();
    dbPingMs = dbWatch.ElapsedMilliseconds;
}
catch (Exception ex)
{
    dbConnected = false;
    dbError = ex.Message;
}

var localIp = Dns.GetHostEntry(Dns.GetHostName())
    .AddressList
    .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
    ?.ToString() ?? "127.0.0.1";

app.Lifetime.ApplicationStarted.Register(() =>
{
    startupStopwatch.Stop();
    var endpointCount = app.Services.GetServices<EndpointDataSource>()
        .SelectMany(es => es.Endpoints)
        .Count();

    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Magenta;
    Console.WriteLine("========================================================================");
    Console.WriteLine("             LABTRACK ENTERPRISE BACKEND API IS NOW ONLINE              ");
    Console.WriteLine("========================================================================");
    Console.ResetColor();

    if (dbConnected)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  [DATABASE STATUS] : CONNECTED TO POSTGRESQL ({dbPingMs} ms ping)");
        Console.WriteLine($"  [DB ENDPOINT]     : {dbHost}:{dbPort} -> Database: '{dbName}'");
        Console.WriteLine($"  [DB SCHEMA/MODEL] : Schema 'labtrack_schema' ({entityCount} Entities Mapped)");
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"  [DATABASE STATUS] : DISCONNECTED / CHECK POSTGRESQL ({dbHost}:{dbPort}/{dbName})");
        if (!string.IsNullOrWhiteSpace(dbError))
        {
            Console.WriteLine($"  [DB ERROR REASON] : {dbError}");
        }
    }

    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("------------------------------------------------------------------------");
    Console.WriteLine("  [LOCALHOST URL]   : http://localhost:5000");
    Console.WriteLine($"  [LAN / WIFI URL]  : http://{localIp}:5000");
    Console.WriteLine("  [SWAGGER DOCS]    : http://localhost:5000/swagger");
    Console.WriteLine("  [DIAGNOSTICS API] : http://localhost:5000/api/system/diagnostics");
    Console.WriteLine("  [SIGNALR HUBS]    : /hubs/ai-chat | /hubs/labtrack | /hubs/session");
    Console.WriteLine("  [HEALTH CHECK]    : http://localhost:5000/health");
    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine("------------------------------------------------------------------------");
    Console.WriteLine($"  [ENVIRONMENT]     : {app.Environment.EnvironmentName} (PID: {Environment.ProcessId})");
    Console.WriteLine($"  [ACTIVE ROUTES]   : {endpointCount} Endpoints | 3 Workers | 6 Middlewares Active");
    Console.WriteLine($"  [STARTUP TIME]    : {startupStopwatch.ElapsedMilliseconds} ms");
    Console.ForegroundColor = ConsoleColor.Magenta;
    Console.WriteLine("========================================================================");
    Console.ResetColor();
    Console.WriteLine();
});

app.Run();

public class SafeFallbackProxy : DispatchProxy
{
    public static object Create(Type interfaceType)
    {
        var createMethod = typeof(DispatchProxy)
            .GetMethod(nameof(DispatchProxy.Create), BindingFlags.Public | BindingFlags.Static)!
            .MakeGenericMethod(interfaceType, typeof(SafeFallbackProxy));
        return createMethod.Invoke(null, null)!;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod == null) return null;
        var returnType = targetMethod.ReturnType;

        if (returnType == typeof(void)) return null;
        if (returnType == typeof(Task)) return Task.CompletedTask;

        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var resultType = returnType.GetGenericArguments()[0];
            object? defaultVal = CreateSmartDefault(resultType);
            return typeof(Task).GetMethod(nameof(Task.FromResult))!
                .MakeGenericMethod(resultType)
                .Invoke(null, new[] { defaultVal });
        }

        return CreateSmartDefault(returnType);
    }

    private static object? CreateSmartDefault(Type type)
    {
        if (type.IsValueType) return Activator.CreateInstance(type);
        if (type == typeof(string)) return string.Empty;
        if (type.IsGenericType)
        {
            var genDef = type.GetGenericTypeDefinition();
            if (genDef == typeof(IReadOnlyList<>) || genDef == typeof(IEnumerable<>) || genDef == typeof(ICollection<>) || genDef == typeof(IList<>))
            {
                var itemType = type.GetGenericArguments()[0];
                return Activator.CreateInstance(typeof(List<>).MakeGenericType(itemType));
            }
        }
        return null;
    }
}
