using LabTrack.Application.Interfaces;

namespace LabTrack.Application.Services;

public class AiAssistantService : IAiAssistantService
{
    private readonly IPythonAiClient _pythonAiClient;

    public AiAssistantService(IPythonAiClient pythonAiClient)
    {
        _pythonAiClient = pythonAiClient;
    }

    public async Task<string> AskQuestionAsync(
        string question, 
        object? contextData = null, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            throw new ArgumentException("Question cannot be empty.", nameof(question));
        }

        return await _pythonAiClient.ExecutePromptAsync(question, contextData, cancellationToken);
    }

    public async Task<TResponse?> GetStructuredAdviceAsync<TResponse>(
        string query, 
        object? contextData = null, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Query cannot be empty.", nameof(query));
        }

        return await _pythonAiClient.ExecuteStructuredPromptAsync<TResponse>(query, contextData, cancellationToken);
    }

    public async Task<string> GetAssetDiagnosticsAsync(
        Guid assetId, 
        object telemetryData, 
        CancellationToken cancellationToken = default)
    {
        if (assetId == Guid.Empty)
        {
            throw new ArgumentException("Asset ID must be provided.", nameof(assetId));
        }

        return await _pythonAiClient.AnalyzeAssetMetricsAsync(assetId, telemetryData, cancellationToken);
    }

    public async Task<bool> CheckServiceAvailabilityAsync(CancellationToken cancellationToken = default)
    {
        return await _pythonAiClient.IsServiceHealthyAsync(cancellationToken);
    }

    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
    // BAGONG METHODS â€” para matugunan ang IAiAssistantService
    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

    public async Task<string> GetChatResponseAsync(
        Guid userId,
        Guid sessionId,
        string message,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID must be provided.", nameof(userId));
        }

        if (sessionId == Guid.Empty)
        {
            throw new ArgumentException("Session ID must be provided.", nameof(sessionId));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Message cannot be empty.", nameof(message));
        }

        var contextData = new
        {
            UserId = userId,
            SessionId = sessionId,
            Timestamp = DateTime.UtcNow
        };

        return await _pythonAiClient.ExecutePromptAsync(message, contextData, cancellationToken);
    }

    public async Task<string> AnalyzeProblemReportAsync(
        Guid reportId,
        string additionalContext,
        CancellationToken cancellationToken = default)
    {
        if (reportId == Guid.Empty)
        {
            throw new ArgumentException("Report ID must be provided.", nameof(reportId));
        }

        var prompt = "Analyze the following problem report and provide diagnostic insights, " +
                     "potential root causes, and recommended actions.";

        var contextData = new
        {
            ReportId = reportId,
            AdditionalContext = additionalContext,
            AnalysisType = "ProblemReport"
        };

        return await _pythonAiClient.ExecutePromptAsync(prompt, contextData, cancellationToken);
    }

    public async Task<string> GenerateMaintenanceSummaryAsync(
        Guid assetId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        if (assetId == Guid.Empty)
        {
            throw new ArgumentException("Asset ID must be provided.", nameof(assetId));
        }

        if (fromUtc >= toUtc)
        {
            throw new ArgumentException("'fromUtc' must be earlier than 'toUtc'.");
        }

        var prompt = "Generate a comprehensive maintenance summary for the specified asset " +
                     "within the given date range, including issues encountered, " +
                     "actions taken, and recommendations.";

        var contextData = new
        {
            AssetId = assetId,
            FromUtc = fromUtc,
            ToUtc = toUtc,
            SummaryType = "MaintenanceSummary"
        };

        return await _pythonAiClient.ExecutePromptAsync(prompt, contextData, cancellationToken);
    }
}