using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LabTrack.Api.Health;

public sealed class PythonAiHealthCheck : IHealthCheck
{
    private readonly HttpClient _httpClient;
    private readonly string _fastApiBaseUrl;

    public PythonAiHealthCheck(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _fastApiBaseUrl = configuration["Services:FastApiUrl"] 
            ?? configuration["FastApi:BaseUrl"] 
            ?? "http://localhost:8000";
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var requestUrl = $"{_fastApiBaseUrl.TrimEnd('/')}/health";
            using var response = await _httpClient.GetAsync(requestUrl, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy("Python FastAPI internal AI service is operational.");
            }

            return HealthCheckResult.Degraded(
                $"Python FastAPI service responded with non-success status code: {response.StatusCode}.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Python FastAPI internal AI service health check failed or connection was refused.", ex);
        }
    }
}