namespace LabTrack.Application.Interfaces;

public interface IAiAssistantService
{
    Task<string> GetChatResponseAsync(
        Guid userId, 
        Guid sessionId, 
        string userPrompt, 
        CancellationToken cancellationToken = default);

    Task<string> AnalyzeProblemReportAsync(
        Guid problemReportId, 
        string description, 
        CancellationToken cancellationToken = default);

    Task<string> GenerateMaintenanceSummaryAsync(
        Guid laboratoryId, 
        DateTime startUtc, 
        DateTime endUtc, 
        CancellationToken cancellationToken = default);
}