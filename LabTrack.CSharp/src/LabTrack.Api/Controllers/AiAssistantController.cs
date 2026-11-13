using LabTrack.Application.Interfaces;
using LabTrack.Api.Contracts;
using LabTrack.Domain.Entities;
using LabTrack.Infrastructure.Persistence.DbContext;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LabTrack.Api.Controllers;

public interface IPythonAiClient
{
    Task<AiChatResponseDto> SendChatAsync(AiChatRequestDto request, CancellationToken cancellationToken = default);
    Task<PredictiveMaintenanceResponseDto> GetPredictiveMaintenanceAsync(PredictiveMaintenanceRequestDto request, CancellationToken cancellationToken = default);
    Task<RepairCostEstimationResponseDto> EstimateRepairCostAsync(RepairCostEstimationRequestDto request, CancellationToken cancellationToken = default);
}

public sealed record AiChatRequestDto(string Prompt, string? SystemContext = null, string? SessionId = null);
public sealed record AiChatResponseDto(string Response, double ProcessingTimeMs);

public sealed record PredictiveMaintenanceRequestDto(Guid AssetId, int SystemUsageHours);
public sealed record PredictiveMaintenanceResponseDto(Guid AssetId, double FailureRiskPercentage, string Recommendation, DateTime NextMaintenanceDueDate);

public sealed record RepairCostEstimationRequestDto(Guid ProblemReportId, string IssueDescription, string HardwareModel);
public sealed record RepairCostEstimationResponseDto(Guid ProblemReportId, decimal EstimatedCost, string Currency, string LaborComplexity);

[ApiController]
[Route("api/v1/ai")]
[AllowAnonymous]
public sealed class AiAssistantController : ControllerBase
{
    private readonly IPythonAiClient _pythonAiClient;
    private readonly LabTrackDbContext _dbContext;
    private readonly ILogger<AiAssistantController> _logger;

    public AiAssistantController(
        IPythonAiClient pythonAiClient,
        LabTrackDbContext dbContext,
        ILogger<AiAssistantController> logger)
    {
        _pythonAiClient = pythonAiClient;
        _dbContext = dbContext;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpPost("chat")]
    public async Task<ActionResult<ApiResponse<AiChatResponseDto>>> Chat(
        [FromBody] AiChatRequestDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            return BadRequest(ApiResponse<AiChatResponseDto>.Fail(
                "INVALID_PROMPT",
                "Prompt cannot be empty."));
        }

        try
        {
            var response = await _pythonAiClient.SendChatAsync(request, cancellationToken);

            await PersistChatAsync(request.Prompt, request.SessionId, response, cancellationToken);

            return Ok(ApiResponse<AiChatResponseDto>.Ok(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process AI chat request");
            return StatusCode(500, ApiResponse<AiChatResponseDto>.Fail(
                "AI_SERVICE_ERROR",
                "An error occurred while communicating with the AI gateway service."));
        }
    }

    // â•â•â• NEW AI ROUTES â•â•â•
    [AllowAnonymous]
    [HttpPost("chat/query")]
    public Task<ActionResult<ApiResponse<AiChatResponseDto>>> ChatQuery(
        [FromBody] AiChatRequestDto request, CancellationToken cancellationToken)
        => Chat(request, cancellationToken);

    [AllowAnonymous]
    [HttpPost("damage-estimate")]
    public async Task<ActionResult<ApiResponse<RepairCostEstimationResponseDto>>> DamageEstimate(
        [FromBody] RepairCostEstimationRequestDto request, CancellationToken cancellationToken)
    {
        try {
            var r = await _pythonAiClient.EstimateRepairCostAsync(request, cancellationToken);
            return Ok(ApiResponse<RepairCostEstimationResponseDto>.Ok(r));
        } catch (Exception ex) {
            _logger.LogError(ex, "damage-estimate error");
            return StatusCode(500, ApiResponse<RepairCostEstimationResponseDto>.Fail("AI_SERVICE_ERROR", "Error."));
        }
    }

    [AllowAnonymous]
    [HttpPost("predictive-maintenance")]
    public async Task<ActionResult<ApiResponse<PredictiveMaintenanceResponseDto>>> PredictiveMaintenance(
        [FromBody] PredictiveMaintenanceRequestDto request, CancellationToken cancellationToken)
    {
        try {
            var r = await _pythonAiClient.GetPredictiveMaintenanceAsync(request, cancellationToken);
            return Ok(ApiResponse<PredictiveMaintenanceResponseDto>.Ok(r));
        } catch (Exception ex) {
            _logger.LogError(ex, "predictive-maintenance error");
            return StatusCode(500, ApiResponse<PredictiveMaintenanceResponseDto>.Fail("AI_SERVICE_ERROR", "Error."));
        }
    }

    [AllowAnonymous]
    [HttpGet("sessions")]
    public async Task<ActionResult<ApiResponse<List<object>>>> GetSessions(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Ok(ApiResponse<List<object>>.Ok(new List<object>()));
        try {
            var sessions = await _dbContext.AiChatSessions
                .Where(s => s.UserId == userId && s.IsActive)
                .OrderByDescending(s => s.CreatedAtUtc).Take(20)
                .ToListAsync(cancellationToken);
            var result = sessions.Select(s => (object)new {
                id = s.Id.ToString(),
                title = s.Title,
                category = "General AI Core",
                lastActive = s.CreatedAtUtc.ToString("h:mm tt"),
                msgCount = 0,
                messages = new List<object>()
            }).ToList();
            return Ok(ApiResponse<List<object>>.Ok(result));
        } catch (Exception ex) {
            _logger.LogError(ex, "get sessions error");
            return Ok(ApiResponse<List<object>>.Ok(new List<object>()));
        }
    }
    private async Task PersistChatAsync(
        string question,
        string? sessionIdStr,
        AiChatResponseDto response,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("sub")?.Value;

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            _logger.LogInformation("AI chat not persisted (guest/no user claim).");
            return;
        }

        try
        {
            Guid? requestedSessionId = null;
            if (!string.IsNullOrWhiteSpace(sessionIdStr) &&
                Guid.TryParse(sessionIdStr, out var parsedSid))
            {
                requestedSessionId = parsedSid;
            }

            AiChatSession? session = null;
            if (requestedSessionId.HasValue)
            {
                session = await _dbContext.AiChatSessions
                    .FirstOrDefaultAsync(s => s.Id == requestedSessionId.Value, cancellationToken);
            }

            if (session == null)
            {
                session = await _dbContext.AiChatSessions
                    .FirstOrDefaultAsync(s => s.UserId == userId && s.IsActive, cancellationToken);
            }

            if (session == null)
            {
                var title = question.Length > 100 ? question.Substring(0, 100) : question;
                session = new AiChatSession
                {
                    Id = requestedSessionId ?? Guid.NewGuid(),
                    UserId = userId,
                    Title = title,
                    StartedAtUtc = DateTime.UtcNow,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow
                };
                _dbContext.AiChatSessions.Add(session);
            }

            var safeQ = question.Length > 4000 ? question.Substring(0, 4000) : question;
            var safeA = response.Response.Length > 8000
                ? response.Response.Substring(0, 8000) : response.Response;

            _dbContext.AiChatMessages.Add(new AiChatMessage
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SessionId = session.Id,
                Question = safeQ,
                Answer = safeA,
                ResponseTimeMs = response.ProcessingTimeMs,
                PromptTokens = 0,
                CompletionTokens = 0,
                CreatedAtUtc = DateTime.UtcNow
            });

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "AI chat persisted: user={UserId}, session={SessionId}, qLen={QLen}",
                userId, session.Id, safeQ.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PersistChatAsync failed for user {UserId}", userId);
        }
    }
}

