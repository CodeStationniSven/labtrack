using LabTrack.Application.Interfaces;
using System.Text.Json.Serialization;
using LabTrack.Api.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LabTrack.Api.Controllers;

public sealed record ProblemReportDto(
    Guid Id,
    Guid? WorkstationId,
    string WorkstationNumber,
    string LabName,
    Guid ReportedByUserId,
    string ReportedByName,
    [property: JsonPropertyName("category")] string Category,
    string Title,
    string Description,
    string Priority,
    string Status,
    decimal? EstimatedRepairCost,
    DateTime ReportedAt,
    DateTime? ResolvedAt
);

public sealed record CreateProblemReportDto(
    string? WorkstationId,
    string? WorkstationCode,
    string? LaboratoryId,
    string? LaboratoryName,
    string? LabName,
    string? Title,
    string? Category,
    string? Description,
    string? Priority
);

public sealed record UpdateProblemReportStatusDto(
    string Status,
    decimal? EstimatedRepairCost,
    string? AdminNotes
);

public sealed record UpdateProblemReportDto(
    string? Title,
    string? Description,
    string? Category,
    string? Priority,
    string? Status,
    string? ResolutionNotes
);

[ApiController]
[Route("api/v1/problem-reports")]
[Authorize]
public sealed class ProblemReportController : ControllerBase
{
    private readonly IProblemReportService _problemReportService;
    private readonly ILogger<ProblemReportController> _logger;

    public ProblemReportController(
        IProblemReportService problemReportService,
        ILogger<ProblemReportController> logger)
    {
        _problemReportService = problemReportService;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "TECHNICIAN,INSTRUCTOR,ADMIN,ADMIN_TECHNICIAN,SUPER_ADMIN,LAB_ADMIN")]
    public async Task<ActionResult<ApiResponse<PaginationResponse<ProblemReportDto>>>> GetProblemReports(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? status = null,
        [FromQuery] string? category = null,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1 || pageSize < 1)
        {
            return BadRequest(ApiResponse<PaginationResponse<ProblemReportDto>>.Fail(
                "INVALID_PAGINATION", "Page number and page size must be greater than zero."));
        }
        try
        {
            var result = await _problemReportService.GetProblemReportsAsync(pageNumber, pageSize, status, category, cancellationToken);
            return Ok(ApiResponse<PaginationResponse<ProblemReportDto>>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching problem reports list");
            return StatusCode(500, ApiResponse<PaginationResponse<ProblemReportDto>>.Fail(
                "PROBLEM_REPORT_ERROR", "An error occurred while fetching problem reports."));
        }
    }

    [HttpGet("mine")]
    [Authorize(Roles = "TECHNICIAN,INSTRUCTOR,ADMIN,ADMIN_TECHNICIAN,SUPER_ADMIN,LAB_ADMIN")]
    public async Task<ActionResult<ApiResponse<PaginationResponse<ProblemReportDto>>>> GetMyProblemReports(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                           ?? User.FindFirst("sub")?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized(ApiResponse<PaginationResponse<ProblemReportDto>>.Fail(
                    "UNAUTHORIZED", "Invalid user identity."));
            var result = await _problemReportService.GetMyProblemReportsAsync(userId, pageNumber, pageSize, cancellationToken);
            return Ok(ApiResponse<PaginationResponse<ProblemReportDto>>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching my problem reports");
            return StatusCode(500, ApiResponse<PaginationResponse<ProblemReportDto>>.Fail(
                "PROBLEM_REPORT_ERROR", "Error fetching my reports."));
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProblemReportDto>>> GetProblemReportById(
        Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var report = await _problemReportService.GetProblemReportByIdAsync(id, cancellationToken);
            if (report is null)
                return NotFound(ApiResponse<ProblemReportDto>.Fail(
                    "REPORT_NOT_FOUND", $"Problem report with ID '{id}' was not found."));
            return Ok(ApiResponse<ProblemReportDto>.Ok(report));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching problem report with ID {ReportId}", id);
            return StatusCode(500, ApiResponse<ProblemReportDto>.Fail(
                "PROBLEM_REPORT_ERROR", "An error occurred while fetching the problem report details."));
        }
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<ProblemReportDto>>> SubmitProblemReport(
        [FromBody] CreateProblemReportDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Category) || string.IsNullOrWhiteSpace(request.Description))
        {
            return BadRequest(ApiResponse<ProblemReportDto>.Fail(
                "INVALID_REPORT_DATA",
                "Issue category and description are required."));
        }
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(userIdClaim, out var reportedByUserId))
        {
            return Unauthorized(ApiResponse<ProblemReportDto>.Fail(
                "UNAUTHORIZED", "User identity is invalid or missing."));
        }
        try
        {
            var createdReport = await _problemReportService.SubmitProblemReportAsync(request, reportedByUserId, cancellationToken);
            return CreatedAtAction(
                nameof(GetProblemReportById),
                new { id = createdReport.Id },
                ApiResponse<ProblemReportDto>.Ok(createdReport));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error submitting report");
            return BadRequest(ApiResponse<ProblemReportDto>.Fail("INVALID_REPORT_DATA", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting problem report");
            return StatusCode(500, ApiResponse<ProblemReportDto>.Fail(
                "PROBLEM_REPORT_CREATE_ERROR", "An error occurred while submitting the problem report."));
        }
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "TECHNICIAN,ADMIN,ADMIN_TECHNICIAN,SUPER_ADMIN,LAB_ADMIN")]
    public async Task<ActionResult<ApiResponse<ProblemReportDto>>> UpdateReportStatus(
        Guid id, [FromBody] UpdateProblemReportStatusDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Status))
            return BadRequest(ApiResponse<ProblemReportDto>.Fail(
                "INVALID_STATUS_DATA", "Status field is required."));
        try
        {
            var updatedReport = await _problemReportService.UpdateReportStatusAsync(id, request, cancellationToken);
            if (updatedReport is null)
                return NotFound(ApiResponse<ProblemReportDto>.Fail(
                    "REPORT_NOT_FOUND", $"Problem report with ID '{id}' was not found."));
            return Ok(ApiResponse<ProblemReportDto>.Ok(updatedReport));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status for problem report ID {ReportId}", id);
            return StatusCode(500, ApiResponse<ProblemReportDto>.Fail(
                "PROBLEM_REPORT_UPDATE_ERROR", "An error occurred while updating the problem report status."));
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "TECHNICIAN,ADMIN,ADMIN_TECHNICIAN,SUPER_ADMIN,LAB_ADMIN,INSTRUCTOR")]
    public async Task<ActionResult<ApiResponse<ProblemReportDto>>> UpdateProblemReport(
        Guid id, [FromBody] UpdateProblemReportDto request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _problemReportService.UpdateProblemReportAsync(id, request, cancellationToken);
            if (result is null)
                return NotFound(ApiResponse<ProblemReportDto>.Fail(
                    "REPORT_NOT_FOUND", $"Report {id} not found."));
            return Ok(ApiResponse<ProblemReportDto>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating problem report {ReportId}", id);
            return StatusCode(500, ApiResponse<ProblemReportDto>.Fail(
                "PROBLEM_REPORT_UPDATE_ERROR", "Error updating report."));
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "TECHNICIAN,ADMIN,ADMIN_TECHNICIAN,SUPER_ADMIN,LAB_ADMIN")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteProblemReport(
        Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _problemReportService.DeleteProblemReportAsync(id, cancellationToken);
            if (!deleted)
                return NotFound(ApiResponse<bool>.Fail(
                    "REPORT_NOT_FOUND", $"Problem report with ID '{id}' was not found."));
            return Ok(ApiResponse<bool>.Ok(true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting problem report ID {ReportId}", id);
            return StatusCode(500, ApiResponse<bool>.Fail(
                "PROBLEM_REPORT_DELETE_ERROR", "An error occurred while deleting the problem report."));
        }
    }
}

