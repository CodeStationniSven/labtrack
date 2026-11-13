using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LabTrack.Api.Contracts;
using LabTrack.Application.Dtos.Auth;
using LabTrack.Application.Interfaces;

namespace LabTrack.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _authService.LoginAsync(request, cancellationToken);
            return Ok(ApiResponse<AuthResponseDto>.Ok(result));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<AuthResponseDto>.Fail("AUTH_FAILED", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed");
            return BadRequest(ApiResponse<AuthResponseDto>.Fail("VALIDATION_ERROR", ex.Message));
        }
    }

        [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Register(
        [FromBody] RegisterStaffDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _authService.RegisterAsync(request, cancellationToken);
            return Ok(ApiResponse<AuthResponseDto>.Ok(result));
        }
        catch (FluentValidation.ValidationException vex)
        {
            var msg = string.Join("; ", vex.Errors.Select(e => e.ErrorMessage));
            _logger.LogWarning("Registration validation failed: {Msg}", msg);
            return BadRequest(ApiResponse<AuthResponseDto>.Fail("VALIDATION_FAILED", msg));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration failed for {Email}", request.Email);
            return BadRequest(ApiResponse<AuthResponseDto>.Fail(
                "REGISTRATION_FAILED",
                $"[{ex.GetType().Name}] {ex.Message}"));
        }
    }

    [HttpPost("verify-pin")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> VerifyPin([FromBody] VerifyPinDto request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _authService.VerifyPinAsync(request, cancellationToken);
            return Ok(ApiResponse<AuthResponseDto>.Ok(result));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<AuthResponseDto>.Fail("INVALID_PIN", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PIN verify failed");
            return BadRequest(ApiResponse<AuthResponseDto>.Fail("VALIDATION_ERROR", ex.Message));
        }
    }
}