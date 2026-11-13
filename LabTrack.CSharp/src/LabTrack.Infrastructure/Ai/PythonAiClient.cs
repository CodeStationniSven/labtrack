using System.Net.Http.Json;
using System.Text.Json;
using LabTrack.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LabTrack.Infrastructure.Ai;

public class PythonAiClientOptions
{
    public const string SectionName = "PythonAiClient";

    public string BaseUrl { get; set; } = "http://localhost:8000";
    public string ApiKey { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
}

public class PythonAiRequest
{
    public string Prompt { get; set; } = string.Empty;
    public string SystemContext { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public class PythonAiResponse
{
    public bool Success { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

public class PythonAiClient : IPythonAiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PythonAiClient> _logger;
    private readonly PythonAiClientOptions _options;

    public PythonAiClient(
        HttpClient httpClient,
        IOptions<PythonAiClientOptions> options,
        ILogger<PythonAiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;

        ConfigureHttpClient();
    }

    private void ConfigureHttpClient()
    {
        if (_httpClient.BaseAddress is null && !string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        }

        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);

        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Remove("X-API-Key");
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", _options.ApiKey);
        }
    }

    public async Task<PythonAiResponse> GenerateResponseAsync(
        PythonAiRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/generate", request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<PythonAiResponse>(cancellationToken: cancellationToken);
                return result ?? new PythonAiResponse
                {
                    Success = false,
                    ErrorMessage = "Received empty response body from Python AI service."
                };
            }

            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Python AI service failed with status code {StatusCode}: {ErrorBody}", response.StatusCode, errorBody);

            return new PythonAiResponse
            {
                Success = false,
                ErrorMessage = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}"
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request error occurred while communicating with Python AI service.");
            return new PythonAiResponse
            {
                Success = false,
                ErrorMessage = $"Connection failure: {ex.Message}"
            };
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Request to Python AI service timed out after {Timeout} seconds.", _options.TimeoutSeconds);
            return new PythonAiResponse
            {
                Success = false,
                ErrorMessage = "Request to AI microservice timed out."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred during Python AI service call.");
            return new PythonAiResponse
            {
                Success = false,
                ErrorMessage = $"An unexpected error occurred: {ex.Message}"
            };
        }
    }

    public async Task<string> ExecutePromptAsync(
        string prompt,
        object? context = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new ArgumentException("Prompt cannot be null or empty.", nameof(prompt));
        }

        var request = new PythonAiRequest
        {
            Prompt = prompt,
            SystemContext = SerializeContext(context)
        };

        var response = await GenerateResponseAsync(request, cancellationToken);

        if (!response.Success)
        {
            throw new InvalidOperationException(
                response.ErrorMessage ?? "Python AI prompt execution failed.");
        }

        return response.Content;
    }

    public async Task<TResponse?> ExecuteStructuredPromptAsync<TResponse>(
        string prompt,
        object? context = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new ArgumentException("Prompt cannot be null or empty.", nameof(prompt));
        }

        var request = new PythonAiRequest
        {
            Prompt = prompt,
            SystemContext = SerializeContext(context),
            Parameters = new Dictionary<string, object> { ["response_format"] = "json" }
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "api/v1/generate", request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "Python AI structured prompt failed with status code {StatusCode}: {ErrorBody}",
                    response.StatusCode, errorBody);
                return default;
            }

            var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(rawJson))
            {
                return default;
            }

            try
            {
                return JsonSerializer.Deserialize<TResponse>(rawJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (JsonException)
            {
                var wrapped = JsonSerializer.Deserialize<PythonAiResponse>(rawJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (wrapped?.Content is null)
                {
                    return default;
                }

                return JsonSerializer.Deserialize<TResponse>(wrapped.Content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request error during structured prompt execution.");
            return default;
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex,
                "Structured prompt request timed out after {Timeout} seconds.",
                _options.TimeoutSeconds);
            return default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during structured prompt execution.");
            return default;
        }
    }

    public async Task<string> AnalyzeAssetMetricsAsync(
        Guid assetId,
        object metrics,
        CancellationToken cancellationToken = default)
    {
        if (metrics is null)
        {
            throw new ArgumentNullException(nameof(metrics));
        }

        var prompt =
            $"Analyze the operational metrics for asset {assetId} and provide a concise " +
            "diagnostic summary including: health status, risk indicators, and recommended actions.";

        var request = new PythonAiRequest
        {
            Prompt = prompt,
            SystemContext = SerializeContext(metrics),
            Parameters = new Dictionary<string, object>
            {
                ["asset_id"] = assetId.ToString(),
                ["task"] = "asset_metrics_analysis"
            }
        };

        var response = await GenerateResponseAsync(request, cancellationToken);

        if (!response.Success)
        {
            throw new InvalidOperationException(
                response.ErrorMessage ?? "Asset metrics analysis failed.");
        }

        return response.Content;
    }

    public async Task<bool> IsServiceHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync("health", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Python AI service health probe failed (connection error).");
            return false;
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Python AI service health probe timed out.");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Python AI service health probe failed unexpectedly.");
            return false;
        }
    }

    private static string SerializeContext(object? context)
    {
        if (context is null)
        {
            return string.Empty;
        }

        if (context is string s)
        {
            return s;
        }

        try
        {
            return JsonSerializer.Serialize(context, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });
        }
        catch
        {
            return context.ToString() ?? string.Empty;
        }
    }
}