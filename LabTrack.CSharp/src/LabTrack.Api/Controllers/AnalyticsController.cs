using LabTrack.Application.Interfaces;
using LabTrack.Api.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LabTrack.Api.Controllers;

public sealed record LabUsageStatsDto(
    int TotalSessions,
    double TotalHoursLogged,
    int UniqueStudentsCount,
    double PeakHour
);

public sealed record AssetStatusSummaryDto(
    string Status,
    int Count,
    double Percentage
);

public sealed record FrequentIssueDto(
    string Category,
    int TotalReports,
    decimal TotalEstimatedCost
);

[ApiController]
[Route("api/v1/analytics")]
[Authorize(Roles = "TECHNICIAN,INSTRUCTOR,ADMIN,ADMIN_TECHNICIAN")]
public sealed class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(
        IAnalyticsService analyticsService,
        ILogger<AnalyticsController> logger)
    {
        _analyticsService = analyticsService;
        _logger = logger;
    }

    [HttpGet("usage")]
    public async Task<ActionResult<ApiResponse<LabUsageStatsDto>>> GetUsageStats(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken cancellationToken)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(-30);
        var end = endDate ?? DateTime.UtcNow;

        if (start > end)
        {
            return BadRequest(ApiResponse<LabUsageStatsDto>.Fail(
                "INVALID_DATE_RANGE",
                "Start date cannot be later than end date."));
        }

        try
        {
            var stats = await _analyticsService.GetLabUsageStatsAsync(start, end, cancellationToken);
            return Ok(ApiResponse<LabUsageStatsDto>.Ok(stats));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving lab usage statistics");
            return StatusCode(500, ApiResponse<LabUsageStatsDto>.Fail(
                "ANALYTICS_ERROR",
                "An error occurred while gathering usage statistics."));
        }
    }

    [HttpGet("assets/status-summary")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AssetStatusSummaryDto>>>> GetAssetStatusSummary(
        CancellationToken cancellationToken)
    {
        try
        {
            var summary = await _analyticsService.GetAssetStatusSummaryAsync(cancellationToken);
            return Ok(ApiResponse<IReadOnlyList<AssetStatusSummaryDto>>.Ok(summary));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving asset status summary");
            return StatusCode(500, ApiResponse<IReadOnlyList<AssetStatusSummaryDto>>.Fail(
                "ANALYTICS_ERROR",
                "An error occurred while gathering asset status summary."));
        }
    }

    [HttpGet("issues/frequent")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<FrequentIssueDto>>>> GetFrequentIssues(
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        if (limit <= 0 || limit > 100)
        {
            return BadRequest(ApiResponse<IReadOnlyList<FrequentIssueDto>>.Fail(
                "INVALID_LIMIT",
                "Limit must be between 1 and 100."));
        }

        try
        {
            var issues = await _analyticsService.GetFrequentIssuesAsync(limit, cancellationToken);
            return Ok(ApiResponse<IReadOnlyList<FrequentIssueDto>>.Ok(issues));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving frequent problem reports");
            return StatusCode(500, ApiResponse<IReadOnlyList<FrequentIssueDto>>.Fail(
                "ANALYTICS_ERROR",
                "An error occurred while gathering frequent issues."));
        }
    }
}

