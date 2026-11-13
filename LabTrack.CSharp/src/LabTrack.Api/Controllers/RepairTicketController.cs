using LabTrack.Application.Interfaces;
using LabTrack.Api.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LabTrack.Api.Controllers;

public sealed record RepairTicketDto(
    Guid Id,
    Guid ProblemReportId,
    Guid? AssignedTechnicianUserId,
    string? AssignedTechnicianName,
    string Status,
    string Priority,
    string TechnicalNotes,
    decimal ActualRepairCost,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt
);

public sealed record CreateRepairTicketDto(
    Guid AssetId,
    Guid ProblemReportId,    Guid? AssignedTechnicianUserId,
    string Priority,
    string? TechnicalNotes
);

public sealed record UpdateRepairTicketDto(
    Guid? AssignedTechnicianUserId,
    string Status,
    string Priority,
    string? TechnicalNotes,
    decimal? ActualRepairCost
);

[ApiController]
[Route("api/v1/repair-tickets")]
[Authorize(Roles = "TECHNICIAN,ADMIN,ADMIN_TECHNICIAN")]
public sealed class RepairTicketController : ControllerBase
{
    private readonly IRepairTicketService _repairTicketService;
    private readonly ILogger<RepairTicketController> _logger;

    public RepairTicketController(
        IRepairTicketService repairTicketService,
        ILogger<RepairTicketController> logger)
    {
        _repairTicketService = repairTicketService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginationResponse<RepairTicketDto>>>> GetRepairTickets(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        [FromQuery] string? priority = null,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1 || pageSize < 1)
        {
            return BadRequest(ApiResponse<PaginationResponse<RepairTicketDto>>.Fail(
                "INVALID_PAGINATION",
                "Page number and page size must be greater than zero."));
        }

        try
        {
            var result = await _repairTicketService.GetRepairTicketsAsync(pageNumber, pageSize, status, priority, cancellationToken);
            return Ok(ApiResponse<PaginationResponse<RepairTicketDto>>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching repair tickets list");
            return StatusCode(500, ApiResponse<PaginationResponse<RepairTicketDto>>.Fail(
                "REPAIR_TICKET_ERROR",
                "An error occurred while fetching repair tickets."));
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<RepairTicketDto>>> GetRepairTicketById(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var ticket = await _repairTicketService.GetRepairTicketByIdAsync(id, cancellationToken);
            if (ticket is null)
            {
                return NotFound(ApiResponse<RepairTicketDto>.Fail(
                    "TICKET_NOT_FOUND",
                    $"Repair ticket with ID '{id}' was not found."));
            }

            return Ok(ApiResponse<RepairTicketDto>.Ok(ticket));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching repair ticket with ID {TicketId}", id);
            return StatusCode(500, ApiResponse<RepairTicketDto>.Fail(
                "REPAIR_TICKET_ERROR",
                "An error occurred while fetching the repair ticket."));
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<RepairTicketDto>>> CreateRepairTicket(
        [FromBody] CreateRepairTicketDto request,
        CancellationToken cancellationToken)
    {
        if (request.ProblemReportId == Guid.Empty || string.IsNullOrWhiteSpace(request.Priority))
        {
            return BadRequest(ApiResponse<RepairTicketDto>.Fail(
                "INVALID_TICKET_DATA",
                "Problem Report ID and priority are required."));
        }

        try
        {
            var createdTicket = await _repairTicketService.CreateRepairTicketAsync(request, cancellationToken);
            return CreatedAtAction(
                nameof(GetRepairTicketById),
                new { id = createdTicket.Id },
                ApiResponse<RepairTicketDto>.Ok(createdTicket));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating repair ticket for problem report ID {ProblemReportId}", request.ProblemReportId);
            return StatusCode(500, ApiResponse<RepairTicketDto>.Fail("REPAIR_TICKET_CREATE_ERROR", "An error occurred while creating the repair ticket."));
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<RepairTicketDto>>> UpdateRepairTicket(
        Guid id,
        [FromBody] UpdateRepairTicketDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Status))
        {
            return BadRequest(ApiResponse<RepairTicketDto>.Fail(
                "INVALID_TICKET_DATA",
                "Status is required for updating the repair ticket."));
        }

        try
        {
            var updatedTicket = await _repairTicketService.UpdateRepairTicketAsync(id, request, cancellationToken);
            if (updatedTicket is null)
            {
                return NotFound(ApiResponse<RepairTicketDto>.Fail(
                    "TICKET_NOT_FOUND",
                    $"Repair ticket with ID '{id}' was not found."));
            }

            return Ok(ApiResponse<RepairTicketDto>.Ok(updatedTicket));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating repair ticket ID {TicketId}", id);
            return StatusCode(500, ApiResponse<RepairTicketDto>.Fail(
                "REPAIR_TICKET_UPDATE_ERROR",
                "An error occurred while updating the repair ticket."));
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteRepairTicket(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _repairTicketService.DeleteRepairTicketAsync(id, cancellationToken);
            if (!deleted)
            {
                return NotFound(ApiResponse<bool>.Fail(
                    "TICKET_NOT_FOUND",
                    $"Repair ticket with ID '{id}' was not found."));
            }

            return Ok(ApiResponse<bool>.Ok(true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting repair ticket ID {TicketId}", id);
            return StatusCode(500, ApiResponse<bool>.Fail(
                "REPAIR_TICKET_DELETE_ERROR",
                "An error occurred while deleting the repair ticket."));
        }
    }
}


