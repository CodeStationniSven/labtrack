using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LabTrack.Api.Health;

public sealed class ReadinessHealthCheck : IHealthCheck
{
    private readonly HealthCheckService _healthCheckService;

    public ReadinessHealthCheck(HealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var report = await _healthCheckService.CheckHealthAsync(
            check => check.Tags.Contains("db") || check.Tags.Contains("ai"),
            cancellationToken);

        if (report.Status == HealthStatus.Healthy)
        {
            return HealthCheckResult.Healthy("All critical dependency checks (Database, AI Service) passed.");
        }

        if (report.Status == HealthStatus.Degraded)
        {
            return HealthCheckResult.Degraded("One or more non-critical readiness dependencies are degraded.");
        }

        return HealthCheckResult.Unhealthy("Critical dependencies are unavailable for receiving traffic.");
    }
}