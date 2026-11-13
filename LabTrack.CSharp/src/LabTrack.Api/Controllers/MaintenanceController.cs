using LabTrack.Application.Interfaces;
using LabTrack.Api.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LabTrack.Api.Controllers;

public sealed record MaintenanceScheduleDto(
    Guid Id,
    Guid AssetId,
    string MaintenanceType,
    string Status,
    string Description,
    DateTime ScheduledDate,
    DateTime? CompletedAt,
    Guid? PerformedByUserId,
    DateTime CreatedAt
);

public sealed record CreateMaintenanceScheduleDto(
    Guid AssetId,
    string MaintenanceType,
    string Description,
    DateTime ScheduledDate
);

public sealed record UpdateMaintenanceScheduleDto(
    string MaintenanceType,
    string Status,
    string Description,
    DateTime ScheduledDate,
    DateTime? CompletedAt,
    Guid? PerformedByUserId
);

[ApiController]
[Route("api/v1/maintenance")]
[Authorize(Roles = "TECHNICIAN,ADMIN,ADMIN_TECHNICIAN")]
public sealed class MaintenanceController : ControllerBase
{
    private readonly IMaintenanceService _maintenanceService;
    private readonly ILogger<MaintenanceController> _logger;

    public MaintenanceController(
        IMaintenanceService maintenanceService,
        ILogger<MaintenanceController> logger)
    {
        _maintenanceService = maintenanceService;
        _logger = logger;
    }

    [HttpGet("schedules")]
    public async Task<ActionResult<ApiResponse<PaginationResponse<MaintenanceScheduleDto>>>> GetSchedules(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1 || pageSize < 1)
        {
            return BadRequest(ApiResponse<PaginationResponse<MaintenanceScheduleDto>>.Fail(
                "INVALID_PAGINATION",
                "Page number and page size must be greater than zero."));
        }

        try
        {
            var result = await _maintenanceService.GetSchedulesAsync(pageNumber, pageSize, status, cancellationToken);
            return Ok(ApiResponse<PaginationResponse<MaintenanceScheduleDto>>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching maintenance schedules");
            return StatusCode(500, ApiResponse<PaginationResponse<MaintenanceScheduleDto>>.Fail(
                "MAINTENANCE_ERROR",
                "An error occurred while fetching maintenance schedules."));
        }
    }

    [HttpGet("schedules/{id:guid}")]
    public async Task<ActionResult<ApiResponse<MaintenanceScheduleDto>>> GetScheduleById(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var schedule = await _maintenanceService.GetScheduleByIdAsync(id, cancellationToken);
            if (schedule is null)
            {
                return NotFound(ApiResponse<MaintenanceScheduleDto>.Fail(
                    "SCHEDULE_NOT_FOUND",
                    $"Maintenance schedule entry with ID '{id}' was not found."));
            }

            return Ok(ApiResponse<MaintenanceScheduleDto>.Ok(schedule));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching maintenance schedule with ID {ScheduleId}", id);
            return StatusCode(500, ApiResponse<MaintenanceScheduleDto>.Fail(
                "MAINTENANCE_ERROR",
                "An error occurred while fetching the maintenance schedule entry."));
        }
    }

    [HttpPost("schedules")]
    public async Task<ActionResult<ApiResponse<MaintenanceScheduleDto>>> CreateSchedule(
        [FromBody] CreateMaintenanceScheduleDto request,
        CancellationToken cancellationToken)
    {
        if (request.AssetId == Guid.Empty || string.IsNullOrWhiteSpace(request.MaintenanceType))
        {
            return BadRequest(ApiResponse<MaintenanceScheduleDto>.Fail(
                "INVALID_SCHEDULE_DATA",
                "Asset ID and maintenance type are required."));
        }

        try
        {
            var createdSchedule = await _maintenanceService.CreateScheduleAsync(request, cancellationToken);
            return CreatedAtAction(
                nameof(GetScheduleById),
                new { id = createdSchedule.Id },
                ApiResponse<MaintenanceScheduleDto>.Ok(createdSchedule));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating maintenance schedule for asset {AssetId}", request.AssetId);
            return StatusCode(500, ApiResponse<MaintenanceScheduleDto>.Fail(
                "MAINTENANCE_CREATE_ERROR",
                "An error occurred while creating the maintenance schedule entry."));
        }
    }

    [HttpPut("schedules/{id:guid}")]
    public async Task<ActionResult<ApiResponse<MaintenanceScheduleDto>>> UpdateSchedule(
        Guid id,
        [FromBody] UpdateMaintenanceScheduleDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updatedSchedule = await _maintenanceService.UpdateScheduleAsync(id, request, cancellationToken);
            if (updatedSchedule is null)
            {
                return NotFound(ApiResponse<MaintenanceScheduleDto>.Fail(
                    "SCHEDULE_NOT_FOUND",
                    $"Maintenance schedule entry with ID '{id}' was not found."));
            }

            return Ok(ApiResponse<MaintenanceScheduleDto>.Ok(updatedSchedule));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating maintenance schedule ID {ScheduleId}", id);
            return StatusCode(500, ApiResponse<MaintenanceScheduleDto>.Fail(
                "MAINTENANCE_UPDATE_ERROR",
                "An error occurred while updating the maintenance schedule entry."));
        }
    }

    [HttpDelete("schedules/{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteSchedule(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _maintenanceService.DeleteScheduleAsync(id, cancellationToken);
            if (!deleted)
            {
                return NotFound(ApiResponse<bool>.Fail(
                    "SCHEDULE_NOT_FOUND",
                    $"Maintenance schedule entry with ID '{id}' was not found."));
            }

            return Ok(ApiResponse<bool>.Ok(true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting maintenance schedule ID {ScheduleId}", id);
            return StatusCode(500, ApiResponse<bool>.Fail(
                "MAINTENANCE_DELETE_ERROR",
                "An error occurred while deleting the maintenance schedule entry."));
        }
    }
    // ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ AUTO-GENERATED GET ALL ENDPOINT ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬
}

