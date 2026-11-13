using LabTrack.Application.Interfaces;
using LabTrack.Api.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LabTrack.Api.Controllers;

public sealed record AuditLogDto(
    Guid Id,
    string? UserId,
    string Action,
    string EntityName,
    string? EntityId,
    string? Details,
    string? IpAddress,
    DateTime CreatedAt
);

[ApiController]
[Route("api/v1/audit-logs")]
[Authorize(Roles = "TECHNICIAN,ADMIN,ADMIN_TECHNICIAN")]
public sealed class AuditController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<AuditController> _logger;

    public AuditController(
        IAuditLogService auditLogService,
        ILogger<AuditController> logger)
    {
        _auditLogService = auditLogService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginationResponse<AuditLogDto>>>> GetAuditLogs(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? userId = null,
        [FromQuery] string? action = null,
        [FromQuery] string? entityName = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1 || pageSize < 1)
        {
            return BadRequest(ApiResponse<PaginationResponse<AuditLogDto>>.Fail(
                "INVALID_PAGINATION",
                "Page number and page size must be greater than zero."));
        }

        if (startDate.HasValue && endDate.HasValue && startDate > endDate)
        {
            return BadRequest(ApiResponse<PaginationResponse<AuditLogDto>>.Fail(
                "INVALID_DATE_RANGE",
                "Start date cannot be later than end date."));
        }

        try
        {
            var logs = await _auditLogService.GetAuditLogsAsync(
                pageNumber,
                pageSize,
                userId,
                action,
                entityName,
                startDate,
                endDate,
                cancellationToken);

            return Ok(ApiResponse<PaginationResponse<AuditLogDto>>.Ok(logs));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs");
            return StatusCode(500, ApiResponse<PaginationResponse<AuditLogDto>>.Fail(
                "AUDIT_ERROR",
                "An error occurred while retrieving audit logs."));
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AuditLogDto>>> GetAuditLogById(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var log = await _auditLogService.GetAuditLogByIdAsync(id, cancellationToken);
            if (log is null)
            {
                return NotFound(ApiResponse<AuditLogDto>.Fail(
                    "AUDIT_LOG_NOT_FOUND",
                    $"Audit log entry with ID '{id}' was not found."));
            }

            return Ok(ApiResponse<AuditLogDto>.Ok(log));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit log with ID {AuditLogId}", id);
            return StatusCode(500, ApiResponse<AuditLogDto>.Fail(
                "AUDIT_ERROR",
                "An error occurred while retrieving the audit log entry."));
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "ADMIN,ADMIN_TECHNICIAN,SUPER_ADMIN")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteAuditLog(
        Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _auditLogService.DeleteAsync(id, cancellationToken);
            return Ok(ApiResponse<bool>.Ok(true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting audit log {Id}", id);
            return StatusCode(500, ApiResponse<bool>.Fail("AUDIT_DELETE_ERROR", "Error deleting audit log"));
        }
    }
}

