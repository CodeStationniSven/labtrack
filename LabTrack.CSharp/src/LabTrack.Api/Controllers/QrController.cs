using LabTrack.Application.Interfaces;
using LabTrack.Api.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LabTrack.Api.Controllers;

public sealed record QrValidationDto(
    bool IsValid,
    Guid WorkstationId,
    string WorkstationNumber,
    Guid LabId,
    string LabName,
    DateTime ExpiresAt
);

public sealed record QrGenerateRequestDto(
    Guid WorkstationId
);

public sealed record QrGenerateResponseDto(
    string QrToken,
    Guid WorkstationId,
    DateTime GeneratedAt,
    DateTime ExpiresAt
);

[ApiController]
[Route("api/v1/qr")]
public sealed class QrController : ControllerBase
{
    private readonly IQrService _qrService;
    private readonly ILogger<QrController> _logger;

    public QrController(
        IQrService qrService,
        ILogger<QrController> logger)
    {
        _qrService = qrService;
        _logger = logger;
    }

    [HttpGet("validate/{qrToken}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<QrValidationDto>>> ValidateToken(
        string qrToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(qrToken))
        {
            return BadRequest(ApiResponse<QrValidationDto>.Fail(
                "INVALID_QR_TOKEN",
                "QR token parameter cannot be empty."));
        }

        try
        {
            var result = await _qrService.ValidateTokenAsync(qrToken, cancellationToken);
            if (result is null || !result.IsValid)
            {
                return BadRequest(ApiResponse<QrValidationDto>.Fail(
                    "QR_TOKEN_EXPIRED_OR_INVALID",
                    "The provided QR token is invalid or has expired."));
            }

            return Ok(ApiResponse<QrValidationDto>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating QR token {QrToken}", qrToken);
            return StatusCode(500, ApiResponse<QrValidationDto>.Fail(
                "QR_VALIDATION_ERROR",
                "An error occurred while validating the QR token."));
        }
    }

    [HttpPost("generate")]
    [Authorize(Roles = "TECHNICIAN,ADMIN,ADMIN_TECHNICIAN")]
    public async Task<ActionResult<ApiResponse<QrGenerateResponseDto>>> GenerateToken(
        [FromBody] QrGenerateRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request.WorkstationId == Guid.Empty)
        {
            return BadRequest(ApiResponse<QrGenerateResponseDto>.Fail(
                "INVALID_WORKSTATION_ID",
                "Workstation ID is required to generate a QR token."));
        }

        try
        {
            var tokenResponse = await _qrService.GenerateTokenForWorkstationAsync(request.WorkstationId, cancellationToken);
            return Ok(ApiResponse<QrGenerateResponseDto>.Ok(tokenResponse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating QR token for workstation ID {WorkstationId}", request.WorkstationId);
            return StatusCode(500, ApiResponse<QrGenerateResponseDto>.Fail(
                "QR_GENERATE_ERROR",
                "An error occurred while generating the QR token."));
        }
    }
}

