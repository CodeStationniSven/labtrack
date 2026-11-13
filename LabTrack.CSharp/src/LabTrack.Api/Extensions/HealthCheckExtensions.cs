using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;
using System.Text.Json;

namespace LabTrack.Api.Extensions;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddLabTrackHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var dbConnectionString = configuration.GetConnectionString("DefaultConnection");

        var builder = services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" });

        if (!string.IsNullOrWhiteSpace(dbConnectionString))
        {
            builder.AddAsyncCheck("postgresql", async cancellationToken =>
            {
                try
                {
                    await using var connection = new NpgsqlConnection(dbConnectionString);
                    await connection.OpenAsync(cancellationToken);
                    await using var command = connection.CreateCommand();
                    command.CommandText = "SELECT 1;";
                    await command.ExecuteScalarAsync(cancellationToken);

                    return HealthCheckResult.Healthy("PostgreSQL database is available.");
                }
                catch (Exception ex)
                {
                    return HealthCheckResult.Unhealthy("PostgreSQL connection failed.", ex);
                }
            }, tags: new[] { "db", "ready" });
        }

        return services;
    }

    public static IApplicationBuilder UseLabTrackHealthChecks(this IApplicationBuilder app)
    {
        app.UseHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live")
        });

        app.UseHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = WriteHealthCheckResponse
        });

        return app;
    }

    private static Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                component = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds
            }),
            totalDurationMs = report.TotalDuration.TotalMilliseconds
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}