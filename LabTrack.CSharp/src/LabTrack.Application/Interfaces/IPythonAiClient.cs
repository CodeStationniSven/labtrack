namespace LabTrack.Application.Interfaces;

public interface IPythonAiClient
{
    Task<string> ExecutePromptAsync(
        string prompt, 
        object? contextData = null, 
        CancellationToken cancellationToken = default);

    Task<TResponse?> ExecuteStructuredPromptAsync<TResponse>(
        string prompt, 
        object? contextData = null, 
        CancellationToken cancellationToken = default);

    Task<string> AnalyzeAssetMetricsAsync(
        Guid assetId, 
        object telemetryData, 
        CancellationToken cancellationToken = default);

    Task<bool> IsServiceHealthyAsync(
        CancellationToken cancellationToken = default);
}