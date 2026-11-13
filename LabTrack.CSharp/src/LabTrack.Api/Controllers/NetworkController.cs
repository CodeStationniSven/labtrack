using LabTrack.Application.Interfaces;
using LabTrack.Api.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LabTrack.Api.Controllers;

public sealed record NetworkDeviceDto(
    Guid Id,
    Guid LabId,
    string DeviceName,
    string IpAddress,
    string MacAddress,
    string DeviceType,
    string Status,
    DateTime LastSeenAt,
    DateTime CreatedAt
);

public sealed record RegisterNetworkDeviceDto(
    Guid LabId,
    string DeviceName,
    string IpAddress,
    string MacAddress,
    string DeviceType,
    string Status
);

public sealed record UpdateNetworkDeviceDto(
    string DeviceName,
    string IpAddress,
    string MacAddress,
    string DeviceType,
    string Status
);

public sealed record NetworkStatusSummaryDto(
    int TotalDevices,
    int OnlineDevices,
    int OfflineDevices,
    int MaintenanceDevices
);

[ApiController]
[Route("api/v1/network")]
[Authorize(Roles = "TECHNICIAN,ADMIN,ADMIN_TECHNICIAN")]
public sealed class NetworkController : ControllerBase
{
    private readonly INetworkService _networkService;
    private readonly ILogger<NetworkController> _logger;

    public NetworkController(
        INetworkService networkService,
        ILogger<NetworkController> logger)
    {
        _networkService = networkService;
        _logger = logger;
    }

    [HttpGet("devices")]
    public async Task<ActionResult<ApiResponse<PaginationResponse<NetworkDeviceDto>>>> GetNetworkDevices(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? labId = null,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1 || pageSize < 1)
        {
            return BadRequest(ApiResponse<PaginationResponse<NetworkDeviceDto>>.Fail(
                "INVALID_PAGINATION",
                "Page number and page size must be greater than zero."));
        }

        try
        {
            var result = await _networkService.GetNetworkDevicesAsync(pageNumber, pageSize, labId, status, cancellationToken);
            return Ok(ApiResponse<PaginationResponse<NetworkDeviceDto>>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching network devices list");
            return StatusCode(500, ApiResponse<PaginationResponse<NetworkDeviceDto>>.Fail(
                "NETWORK_ERROR",
                "An error occurred while fetching network devices."));
        }
    }

    [HttpGet("devices/{id:guid}")]
    public async Task<ActionResult<ApiResponse<NetworkDeviceDto>>> GetNetworkDeviceById(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var device = await _networkService.GetNetworkDeviceByIdAsync(id, cancellationToken);
            if (device is null)
            {
                return NotFound(ApiResponse<NetworkDeviceDto>.Fail(
                    "DEVICE_NOT_FOUND",
                    $"Network device with ID '{id}' was not found."));
            }

            return Ok(ApiResponse<NetworkDeviceDto>.Ok(device));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching network device with ID {DeviceId}", id);
            return StatusCode(500, ApiResponse<NetworkDeviceDto>.Fail(
                "NETWORK_ERROR",
                "An error occurred while fetching the network device."));
        }
    }

    [HttpPost("devices")]
    public async Task<ActionResult<ApiResponse<NetworkDeviceDto>>> RegisterNetworkDevice(
        [FromBody] RegisterNetworkDeviceDto request,
        CancellationToken cancellationToken)
    {
        if (request.LabId == Guid.Empty || string.IsNullOrWhiteSpace(request.DeviceName) || string.IsNullOrWhiteSpace(request.IpAddress))
        {
            return BadRequest(ApiResponse<NetworkDeviceDto>.Fail(
                "INVALID_DEVICE_DATA",
                "Lab ID, device name, and IP address are required."));
        }

        try
        {
            var createdDevice = await _networkService.RegisterNetworkDeviceAsync(request, cancellationToken);
            return CreatedAtAction(
                nameof(GetNetworkDeviceById),
                new { id = createdDevice.Id },
                ApiResponse<NetworkDeviceDto>.Ok(createdDevice));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering network device");
            return StatusCode(500, ApiResponse<NetworkDeviceDto>.Fail(
                "NETWORK_REGISTER_ERROR",
                "An error occurred while registering the network device."));
        }
    }

    [HttpPut("devices/{id:guid}")]
    public async Task<ActionResult<ApiResponse<NetworkDeviceDto>>> UpdateNetworkDevice(
        Guid id,
        [FromBody] UpdateNetworkDeviceDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updatedDevice = await _networkService.UpdateNetworkDeviceAsync(id, request, cancellationToken);
            if (updatedDevice is null)
            {
                return NotFound(ApiResponse<NetworkDeviceDto>.Fail(
                    "DEVICE_NOT_FOUND",
                    $"Network device with ID '{id}' was not found."));
            }

            return Ok(ApiResponse<NetworkDeviceDto>.Ok(updatedDevice));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating network device with ID {DeviceId}", id);
            return StatusCode(500, ApiResponse<NetworkDeviceDto>.Fail(
                "NETWORK_UPDATE_ERROR",
                "An error occurred while updating the network device."));
        }
    }

    [HttpDelete("devices/{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteNetworkDevice(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _networkService.DeleteNetworkDeviceAsync(id, cancellationToken);
            if (!deleted)
            {
                return NotFound(ApiResponse<bool>.Fail(
                    "DEVICE_NOT_FOUND",
                    $"Network device with ID '{id}' was not found."));
            }

            return Ok(ApiResponse<bool>.Ok(true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting network device with ID {DeviceId}", id);
            return StatusCode(500, ApiResponse<bool>.Fail(
                "NETWORK_DELETE_ERROR",
                "An error occurred while deleting the network device."));
        }
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<NetworkStatusSummaryDto>>> GetNetworkStatusSummary(
        CancellationToken cancellationToken)
    {
        try
        {
            var summary = await _networkService.GetNetworkStatusSummaryAsync(cancellationToken);
            return Ok(ApiResponse<NetworkStatusSummaryDto>.Ok(summary));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching network status summary");
            return StatusCode(500, ApiResponse<NetworkStatusSummaryDto>.Fail(
                "NETWORK_SUMMARY_ERROR",
                "An error occurred while calculating network status summary."));
        }
    }
}

