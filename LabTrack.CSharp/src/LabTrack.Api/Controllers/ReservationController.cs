using LabTrack.Application.Interfaces;
using LabTrack.Api.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LabTrack.Api.Controllers;

public sealed record ReservationDto(
    Guid Id,
    Guid LabId,
    string LabName,
    Guid RequestedByUserId,
    string RequestedByName,
    string Purpose,
    DateTime StartTime,
    DateTime EndTime,
    string Status,
    string? AdminNotes,
    DateTime CreatedAt
);

public sealed record CreateReservationDto(
    Guid LabId,
    string Purpose,
    DateTime StartTime,
    DateTime EndTime
);

public sealed record UpdateReservationStatusDto(
    string Status,
    string? AdminNotes
);

[ApiController]
[Route("api/v1/reservations")]
[Authorize]
public sealed class ReservationController : ControllerBase
{
    private readonly IReservationService _reservationService;
    private readonly ILogger<ReservationController> _logger;

    public ReservationController(
        IReservationService reservationService,
        ILogger<ReservationController> logger)
    {
        _reservationService = reservationService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginationResponse<ReservationDto>>>> GetReservations(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        [FromQuery] Guid? labId = null,
        [FromQuery] Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1 || pageSize < 1)
        {
            return BadRequest(ApiResponse<PaginationResponse<ReservationDto>>.Fail(
                "INVALID_PAGINATION",
                "Page number and page size must be greater than zero."));
        }

        try
        {
            var result = await _reservationService.GetReservationsAsync(pageNumber, pageSize, status, labId, userId, cancellationToken);
            return Ok(ApiResponse<PaginationResponse<ReservationDto>>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching lab reservations list");
            return StatusCode(500, ApiResponse<PaginationResponse<ReservationDto>>.Fail(
                "RESERVATION_ERROR",
                "An error occurred while fetching reservations."));
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ReservationDto>>> GetReservationById(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var reservation = await _reservationService.GetReservationByIdAsync(id, cancellationToken);
            if (reservation is null)
            {
                return NotFound(ApiResponse<ReservationDto>.Fail(
                    "RESERVATION_NOT_FOUND",
                    $"Reservation with ID '{id}' was not found."));
            }

            return Ok(ApiResponse<ReservationDto>.Ok(reservation));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching reservation with ID {ReservationId}", id);
            return StatusCode(500, ApiResponse<ReservationDto>.Fail(
                "RESERVATION_ERROR",
                "An error occurred while fetching the reservation details."));
        }
    }

    [HttpPost]
    [Authorize(Roles = "INSTRUCTOR,TECHNICIAN,ADMIN,ADMIN_TECHNICIAN")]
    public async Task<ActionResult<ApiResponse<ReservationDto>>> CreateReservation(
        [FromBody] CreateReservationDto request,
        CancellationToken cancellationToken)
    {
        if (request.LabId == Guid.Empty || string.IsNullOrWhiteSpace(request.Purpose))
        {
            return BadRequest(ApiResponse<ReservationDto>.Fail(
                "INVALID_RESERVATION_DATA",
                "Lab ID and reservation purpose are required."));
        }

        if (request.StartTime >= request.EndTime)
        {
            return BadRequest(ApiResponse<ReservationDto>.Fail(
                "INVALID_TIME_RANGE",
                "Start time must be strictly earlier than end time."));
        }

        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var requestedByUserId))
        {
            return Unauthorized(ApiResponse<ReservationDto>.Fail(
                "UNAUTHORIZED",
                "User identity is invalid or missing."));
        }

        try
        {
            var createdReservation = await _reservationService.CreateReservationAsync(request, requestedByUserId, cancellationToken);
            return CreatedAtAction(
                nameof(GetReservationById),
                new { id = createdReservation.Id },
                ApiResponse<ReservationDto>.Ok(createdReservation));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating lab reservation for Lab ID {LabId}", request.LabId);
            return StatusCode(500, ApiResponse<ReservationDto>.Fail("RESERVATION_CREATE_ERROR", $"{ex.Message} | Inner: {ex.InnerException?.Message}"));
        }
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "TECHNICIAN,ADMIN,ADMIN_TECHNICIAN")]
    public async Task<ActionResult<ApiResponse<ReservationDto>>> UpdateReservationStatus(
        Guid id,
        [FromBody] UpdateReservationStatusDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Status))
        {
            return BadRequest(ApiResponse<ReservationDto>.Fail(
                "INVALID_STATUS_DATA",
                "Status field is required."));
        }

        try
        {
            var updatedReservation = await _reservationService.UpdateReservationStatusAsync(id, request, cancellationToken);
            if (updatedReservation is null)
            {
                return NotFound(ApiResponse<ReservationDto>.Fail(
                    "RESERVATION_NOT_FOUND",
                    $"Reservation with ID '{id}' was not found."));
            }

            return Ok(ApiResponse<ReservationDto>.Ok(updatedReservation));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status for reservation ID {ReservationId}", id);
            return StatusCode(500, ApiResponse<ReservationDto>.Fail(
                "RESERVATION_UPDATE_ERROR",
                "An error occurred while updating the reservation status."));
        }
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<ApiResponse<bool>>> CancelReservation(
        Guid id,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse<bool>.Fail(
                "UNAUTHORIZED",
                "User identity is invalid or missing."));
        }

        try
        {
            var canceled = await _reservationService.CancelReservationAsync(id, userId, cancellationToken);
            if (!canceled)
            {
                return BadRequest(ApiResponse<bool>.Fail(
                    "RESERVATION_CANCEL_FAILED",
                    $"Reservation with ID '{id}' could not be canceled or was not found."));
            }

            return Ok(ApiResponse<bool>.Ok(true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error canceling reservation ID {ReservationId}", id);
            return StatusCode(500, ApiResponse<bool>.Fail(
                "RESERVATION_CANCEL_ERROR",
                "An error occurred while canceling the reservation."));
        }
    }
    // ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ AUTO-GENERATED GET ALL ENDPOINT ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬
}

