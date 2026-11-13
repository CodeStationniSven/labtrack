using LabTrack.Application.Interfaces;
using LabTrack.Api.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LabTrack.Api.Controllers;

public sealed record ReplacementRequestDto(
    Guid Id,
    Guid AssetId,
    string AssetTag,
    string AssetName,
    Guid RequestedByUserId,
    string RequestedByName,
    string Reason,
    string Status,
    decimal EstimatedReplacementCost,
    DateTime RequestedAt,
    DateTime? ApprovedAt,
    DateTime? ReplacedAt
);

public sealed record CreateReplacementRequestDto(
    Guid AssetId,
    string Reason,
    decimal EstimatedReplacementCost
);

public sealed record UpdateReplacementStatusDto(
    string Status,
    string? AdminNotes
);

[ApiController]
[Route("api/v1/replacements")]
[Authorize(Roles = "TECHNICIAN,ADMIN,ADMIN_TECHNICIAN")]
public sealed class ReplacementController : ControllerBase
{
    private readonly IReplacementService _replacementService;
    private readonly ILogger<ReplacementController> _logger;

    public ReplacementController(
        IReplacementService replacementService,
        ILogger<ReplacementController> logger)
    {
        _replacementService = replacementService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginationResponse<ReplacementRequestDto>>>> GetReplacementRequests(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1 || pageSize < 1)
        {
            return BadRequest(ApiResponse<PaginationResponse<ReplacementRequestDto>>.Fail(
                "INVALID_PAGINATION",
                "Page number and page size must be greater than zero."));
        }

        try
        {
            var result = await _replacementService.GetReplacementRequestsAsync(pageNumber, pageSize, status, cancellationToken);
            return Ok(ApiResponse<PaginationResponse<ReplacementRequestDto>>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching replacement requests list");
            return StatusCode(500, ApiResponse<PaginationResponse<ReplacementRequestDto>>.Fail(
                "REPLACEMENT_ERROR",
                "An error occurred while fetching replacement requests."));
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ReplacementRequestDto>>> GetReplacementRequestById(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var replacement = await _replacementService.GetReplacementRequestByIdAsync(id, cancellationToken);
            if (replacement is null)
            {
                return NotFound(ApiResponse<ReplacementRequestDto>.Fail(
                    "REPLACEMENT_NOT_FOUND",
                    $"Replacement request with ID '{id}' was not found."));
            }

            return Ok(ApiResponse<ReplacementRequestDto>.Ok(replacement));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching replacement request with ID {ReplacementId}", id);
            return StatusCode(500, ApiResponse<ReplacementRequestDto>.Fail(
                "REPLACEMENT_ERROR",
                "An error occurred while fetching the replacement request details."));
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ReplacementRequestDto>>> CreateReplacementRequest(
        [FromBody] CreateReplacementRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request.AssetId == Guid.Empty || string.IsNullOrWhiteSpace(request.Reason))
        {
            return BadRequest(ApiResponse<ReplacementRequestDto>.Fail(
                "INVALID_REPLACEMENT_DATA",
                "Asset ID and replacement reason are required."));
        }

        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var requestedByUserId))
        {
            return Unauthorized(ApiResponse<ReplacementRequestDto>.Fail(
                "UNAUTHORIZED",
                "User identity is invalid or missing."));
        }

        try
        {
            var createdRequest = await _replacementService.CreateReplacementRequestAsync(request, requestedByUserId, cancellationToken);
            return CreatedAtAction(
                nameof(GetReplacementRequestById),
                new { id = createdRequest.Id },
                ApiResponse<ReplacementRequestDto>.Ok(createdRequest));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating replacement request for asset ID {AssetId}", request.AssetId);
            return StatusCode(500, ApiResponse<ReplacementRequestDto>.Fail(
                "REPLACEMENT_CREATE_ERROR",
                "An error occurred while creating the replacement request."));
        }
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse<ReplacementRequestDto>>> UpdateReplacementStatus(
        Guid id,
        [FromBody] UpdateReplacementStatusDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Status))
        {
            return BadRequest(ApiResponse<ReplacementRequestDto>.Fail(
                "INVALID_STATUS_DATA",
                "Status field is required."));
        }

        try
        {
            var updatedRequest = await _replacementService.UpdateReplacementStatusAsync(id, request, cancellationToken);
            if (updatedRequest is null)
            {
                return NotFound(ApiResponse<ReplacementRequestDto>.Fail(
                    "REPLACEMENT_NOT_FOUND",
                    $"Replacement request with ID '{id}' was not found."));
            }

            return Ok(ApiResponse<ReplacementRequestDto>.Ok(updatedRequest));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status for replacement request ID {ReplacementId}", id);
            return StatusCode(500, ApiResponse<ReplacementRequestDto>.Fail(
                "REPLACEMENT_UPDATE_ERROR",
                "An error occurred while updating the replacement request status."));
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteReplacementRequest(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _replacementService.DeleteReplacementRequestAsync(id, cancellationToken);
            if (!deleted)
            {
                return NotFound(ApiResponse<bool>.Fail(
                    "REPLACEMENT_NOT_FOUND",
                    $"Replacement request with ID '{id}' was not found."));
            }

            return Ok(ApiResponse<bool>.Ok(true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting replacement request ID {ReplacementId}", id);
            return StatusCode(500, ApiResponse<bool>.Fail(
                "REPLACEMENT_DELETE_ERROR",
                "An error occurred while deleting the replacement request."));
        }
    }
}

