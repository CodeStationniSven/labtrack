using LabTrack.Application.Interfaces;
using LabTrack.Api.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LabTrack.Api.Controllers;

public sealed record UserDto(
    Guid Id,
    string Username,
    string Email,
    string FullName,
    string Role,
    string? StudentNumber,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? LastLoginAt
);

public sealed record CreateUserDto(
    string Username,
    string Email,
    string Password,
    string FullName,
    string Role,
    string? StudentNumber
);

public sealed record UpdateUserDto(
    string Email,
    string FullName,
    string Role,
    string? StudentNumber,
    bool IsActive
);

public sealed record ToggleUserStatusDto(
    bool IsActive
);

[ApiController]
[Route("api/v1/users")]
[Authorize(Roles = "TECHNICIAN,ADMIN,ADMIN_TECHNICIAN")]
public sealed class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IUserService userService,
        ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginationResponse<UserDto>>>> GetUsers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? role = null,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1 || pageSize < 1)
        {
            return BadRequest(ApiResponse<PaginationResponse<UserDto>>.Fail(
                "INVALID_PAGINATION",
                "Page number and page size must be greater than zero."));
        }

        try
        {
            var result = await _userService.GetUsersAsync(pageNumber, pageSize, search, role, cancellationToken);
            return Ok(ApiResponse<PaginationResponse<UserDto>>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching users list");
            return StatusCode(500, ApiResponse<PaginationResponse<UserDto>>.Fail(
                "USER_ERROR",
                "An error occurred while fetching users."));
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUserById(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userService.GetUserByIdAsync(id, cancellationToken);
            if (user is null)
            {
                return NotFound(ApiResponse<UserDto>.Fail(
                    "USER_NOT_FOUND",
                    $"User with ID '{id}' was not found."));
            }

            return Ok(ApiResponse<UserDto>.Ok(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user with ID {UserId}", id);
            return StatusCode(500, ApiResponse<UserDto>.Fail(
                "USER_ERROR",
                "An error occurred while fetching the user details."));
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser(
        [FromBody] CreateUserDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(ApiResponse<UserDto>.Fail(
                "INVALID_USER_DATA",
                "Username, email, and password are required."));
        }

        try
        {
            var createdUser = await _userService.CreateUserAsync(request, cancellationToken);
            return CreatedAtAction(
                nameof(GetUserById),
                new { id = createdUser.Id },
                ApiResponse<UserDto>.Ok(createdUser));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user {Username}", request.Username);
            return StatusCode(500, ApiResponse<UserDto>.Fail(
                "USER_CREATE_ERROR",
                "An error occurred while creating the user account."));
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUser(
        Guid id,
        [FromBody] UpdateUserDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updatedUser = await _userService.UpdateUserAsync(id, request, cancellationToken);
            if (updatedUser is null)
            {
                return NotFound(ApiResponse<UserDto>.Fail(
                    "USER_NOT_FOUND",
                    $"User with ID '{id}' was not found."));
            }

            return Ok(ApiResponse<UserDto>.Ok(updatedUser));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user ID {UserId}", id);
            return StatusCode(500, ApiResponse<UserDto>.Fail(
                "USER_UPDATE_ERROR",
                "An error occurred while updating user account details."));
        }
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse<bool>>> ToggleUserStatus(
        Guid id,
        [FromBody] ToggleUserStatusDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var success = await _userService.ToggleUserStatusAsync(id, request.IsActive, cancellationToken);
            if (!success)
            {
                return NotFound(ApiResponse<bool>.Fail(
                    "USER_NOT_FOUND",
                    $"User with ID '{id}' was not found."));
            }

            return Ok(ApiResponse<bool>.Ok(true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling status for user ID {UserId}", id);
            return StatusCode(500, ApiResponse<bool>.Fail(
                "USER_STATUS_ERROR",
                "An error occurred while toggling the user account status."));
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteUser(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _userService.DeleteUserAsync(id, cancellationToken);
            if (!deleted)
            {
                return NotFound(ApiResponse<bool>.Fail(
                    "USER_NOT_FOUND",
                    $"User with ID '{id}' was not found."));
            }

            return Ok(ApiResponse<bool>.Ok(true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user ID {UserId}", id);
            return StatusCode(500, ApiResponse<bool>.Fail(
                "USER_DELETE_ERROR",
                "An error occurred while deleting the user account."));
        }
    }
}

