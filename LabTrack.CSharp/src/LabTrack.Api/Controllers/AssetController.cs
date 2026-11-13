using LabTrack.Application.Interfaces;
using LabTrack.Application.Services;
using LabTrack.Api.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LabTrack.Api.Controllers;

public sealed record AssetDto(
    Guid Id,
    string AssetTag,
    string Name,
    string Category,
    string Status,
    string? Location,
    decimal PurchasePrice,
    DateTime PurchasedAt
);

public sealed record CreateAssetDto(
    string AssetTag,
    string Name,
    string Category,
    string Status,
    Guid LaboratoryId,
    string? Location,
    decimal PurchasePrice,
    DateTime PurchasedAt
);

public sealed record UpdateAssetDto(
    string Name,
    string Category,
    string Status,
    string? Location,
    decimal PurchasePrice
);

[ApiController]
[Route("api/v1/assets")]
[Authorize(Roles = "TECHNICIAN,ADMIN,ADMIN_TECHNICIAN,INSTRUCTOR")]
public sealed class AssetController : ControllerBase
{
    private readonly IAssetService _assetService;
    private readonly ILogger<AssetController> _logger;

    public AssetController(
        IAssetService assetService,
        ILogger<AssetController> logger)
    {
        _assetService = assetService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginationResponse<AssetDto>>>> GetAssets(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? category = null,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1 || pageSize < 1)
        {
            return BadRequest(ApiResponse<PaginationResponse<AssetDto>>.Fail(
                "INVALID_PAGINATION",
                "Page number and page size must be greater than zero."));
        }

        try
        {
            var result = await _assetService.GetAssetsAsync(pageNumber, pageSize, search, category, status, cancellationToken);
            return Ok(ApiResponse<PaginationResponse<AssetDto>>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching assets list");
            return StatusCode(500, ApiResponse<PaginationResponse<AssetDto>>.Fail(
                "ASSET_ERROR",
                "An error occurred while fetching assets."));
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AssetDto>>> GetAssetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var asset = await _assetService.GetAssetByIdAsync(id, cancellationToken);
            if (asset is null)
            {
                return NotFound(ApiResponse<AssetDto>.Fail(
                    "ASSET_NOT_FOUND",
                    $"Asset with ID '{id}' was not found."));
            }

            return Ok(ApiResponse<AssetDto>.Ok(asset));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching asset with ID {AssetId}", id);
            return StatusCode(500, ApiResponse<AssetDto>.Fail(
                "ASSET_ERROR",
                "An error occurred while fetching the asset."));
        }
    }

    [HttpPost]

    [Authorize(Roles = "TECHNICIAN,ADMIN,ADMIN_TECHNICIAN")]
    public async Task<ActionResult<ApiResponse<AssetDto>>> CreateAsset(
        [FromBody] CreateAssetDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.AssetTag) || string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(ApiResponse<AssetDto>.Fail(
                "INVALID_ASSET_DATA",
                "Asset tag and name are required."));
        }

        try
        {
            var createdAsset = await _assetService.CreateAssetAsync(request, cancellationToken);
            return CreatedAtAction(
                nameof(GetAssetById),
                new { id = createdAsset.Id },
                ApiResponse<AssetDto>.Ok(createdAsset));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating asset");
            return StatusCode(500, ApiResponse<AssetDto>.Fail(
                "ASSET_CREATE_ERROR",
                "An error occurred while creating the asset."));
        }
    }

    [HttpPut("{id:guid}")]

    [Authorize(Roles = "TECHNICIAN,ADMIN,ADMIN_TECHNICIAN")]
    public async Task<ActionResult<ApiResponse<AssetDto>>> UpdateAsset(
        Guid id,
        [FromBody] UpdateAssetDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updatedAsset = await _assetService.UpdateAssetAsync(id, request, cancellationToken);
            if (updatedAsset is null)
            {
                return NotFound(ApiResponse<AssetDto>.Fail(
                    "ASSET_NOT_FOUND",
                    $"Asset with ID '{id}' was not found."));
            }

            return Ok(ApiResponse<AssetDto>.Ok(updatedAsset));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating asset with ID {AssetId}", id);
            return StatusCode(500, ApiResponse<AssetDto>.Fail(
                "ASSET_UPDATE_ERROR",
                "An error occurred while updating the asset."));
        }
    }

    [HttpDelete("{id:guid}")]

    [Authorize(Roles = "TECHNICIAN,ADMIN,ADMIN_TECHNICIAN")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteAsset(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _assetService.DeleteAssetAsync(id, cancellationToken);
            if (!deleted)
            {
                return NotFound(ApiResponse<bool>.Fail(
                    "ASSET_NOT_FOUND",
                    $"Asset with ID '{id}' was not found."));
            }

            return Ok(ApiResponse<bool>.Ok(true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting asset with ID {AssetId}", id);
            return StatusCode(500, ApiResponse<bool>.Fail(
                "ASSET_DELETE_ERROR",
                "An error occurred while deleting the asset."));
        }
    }
    // ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ AUTO-GENERATED GET ALL ENDPOINT ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬
}

