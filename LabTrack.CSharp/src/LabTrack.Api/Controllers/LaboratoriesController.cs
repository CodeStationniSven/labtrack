using LabTrack.Application.Interfaces;
using LabTrack.Api.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LabTrack.Api.Controllers;

public sealed record LaboratoryDto(
    Guid Id,
    string LabName,
    string RoomNumber,
    string Building,
    int Capacity,
    string Status,
    DateTime CreatedAt
);

public sealed record CreateLaboratoryDto(
    string LabName,
    string RoomNumber,
    string Building,
    int Capacity,
    string Status
);

public sealed record UpdateLaboratoryDto(
    string LabName,
    string RoomNumber,
    string Building,
    int Capacity,
    string Status
);

public sealed record WorkstationDto(
    Guid Id,
    Guid LabId,
    string WorkstationNumber,
    string Status,
    Guid? CurrentAssetId
);

[ApiController]
[Route("api/v1/laboratories")]
[Authorize]
public sealed class LaboratoriesController : ControllerBase
{
    private readonly ILaboratoriesService _laboratoriesService;
    private readonly ILogger<LaboratoriesController> _logger;

    public LaboratoriesController(
        ILaboratoriesService laboratoriesService,
        ILogger<LaboratoriesController> logger)
    {
        _laboratoriesService = laboratoriesService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginationResponse<LaboratoryDto>>>> GetLaboratories(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1 || pageSize < 1)
        {
            return BadRequest(ApiResponse<PaginationResponse<LaboratoryDto>>.Fail(
                "INVALID_PAGINATION",
                "Page number and page size must be greater than zero."));
        }

        try
        {
            var result = await _laboratoriesService.GetLaboratoriesAsync(pageNumber, pageSize, search, status, cancellationToken);
            return Ok(ApiResponse<PaginationResponse<LaboratoryDto>>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching laboratories list");
            return StatusCode(500, ApiResponse<PaginationResponse<LaboratoryDto>>.Fail(
                "LABORATORY_ERROR",
                "An error occurred while fetching laboratories."));
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<LaboratoryDto>>> GetLaboratoryById(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var lab = await _laboratoriesService.GetLaboratoryByIdAsync(id, cancellationToken);
            if (lab is null)
            {
                return NotFound(ApiResponse<LaboratoryDto>.Fail(
                    "LABORATORY_NOT_FOUND",
                    $"Laboratory with ID '{id}' was not found."));
            }

            return Ok(ApiResponse<LaboratoryDto>.Ok(lab));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching laboratory with ID {LabId}", id);
            return StatusCode(500, ApiResponse<LaboratoryDto>.Fail(
                "LABORATORY_ERROR",
                "An error occurred while fetching the laboratory details."));
        }
    }

    [HttpPost]
    [Authorize(Roles = "TECHNICIAN,SUPER_ADMIN,LAB_ADMIN,ADMIN,ADMIN_TECHNICIAN")]
    public async Task<ActionResult<ApiResponse<LaboratoryDto>>> CreateLaboratory(
        [FromBody] CreateLaboratoryDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.LabName) || string.IsNullOrWhiteSpace(request.RoomNumber))
        {
            return BadRequest(ApiResponse<LaboratoryDto>.Fail(
                "INVALID_LAB_DATA",
                "Laboratory name and room number are required."));
        }

        try
        {
            var createdLab = await _laboratoriesService.CreateLaboratoryAsync(request, cancellationToken);
            return CreatedAtAction(
                nameof(GetLaboratoryById),
                new { id = createdLab.Id },
                ApiResponse<LaboratoryDto>.Ok(createdLab));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating laboratory");
            return StatusCode(500, ApiResponse<LaboratoryDto>.Fail(
                "LABORATORY_CREATE_ERROR",
                "An error occurred while creating the laboratory."));
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "TECHNICIAN,SUPER_ADMIN,LAB_ADMIN,ADMIN,ADMIN_TECHNICIAN")]
    public async Task<ActionResult<ApiResponse<LaboratoryDto>>> UpdateLaboratory(
        Guid id,
        [FromBody] UpdateLaboratoryDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updatedLab = await _laboratoriesService.UpdateLaboratoryAsync(id, request, cancellationToken);
            if (updatedLab is null)
            {
                return NotFound(ApiResponse<LaboratoryDto>.Fail(
                    "LABORATORY_NOT_FOUND",
                    $"Laboratory with ID '{id}' was not found."));
            }

            return Ok(ApiResponse<LaboratoryDto>.Ok(updatedLab));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating laboratory with ID {LabId}", id);
            return StatusCode(500, ApiResponse<LaboratoryDto>.Fail(
                "LABORATORY_UPDATE_ERROR",
                "An error occurred while updating the laboratory."));
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "TECHNICIAN,SUPER_ADMIN,LAB_ADMIN,ADMIN,ADMIN_TECHNICIAN")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteLaboratory(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _laboratoriesService.DeleteLaboratoryAsync(id, cancellationToken);
            if (!deleted)
            {
                return NotFound(ApiResponse<bool>.Fail(
                    "LABORATORY_NOT_FOUND",
                    $"Laboratory with ID '{id}' was not found."));
            }

            return Ok(ApiResponse<bool>.Ok(true));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot delete laboratory {LabId} Ã¢â‚¬â€ FK constraint", id);
            return Conflict(ApiResponse<bool>.Fail(
                "LABORATORY_IN_USE",
                ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting laboratory with ID {LabId}", id);
            return StatusCode(500, ApiResponse<bool>.Fail(
                "LABORATORY_DELETE_ERROR",
                "An error occurred while deleting the laboratory."));
        }
    }

    [HttpGet("{id:guid}/workstations")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<WorkstationDto>>>> GetWorkstationsByLabId(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var workstations = await _laboratoriesService.GetWorkstationsByLabIdAsync(id, cancellationToken);
            return Ok(ApiResponse<IReadOnlyList<WorkstationDto>>.Ok(workstations));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching workstations for laboratory ID {LabId}", id);
            return StatusCode(500, ApiResponse<IReadOnlyList<WorkstationDto>>.Fail(
                "LABORATORY_WORKSTATIONS_ERROR",
                "An error occurred while fetching workstations for the laboratory."));
        }
    }
}


