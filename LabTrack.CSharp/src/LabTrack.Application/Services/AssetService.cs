using LabTrack.Application.Dtos.Assets;
using LabTrack.Application.Interfaces;

namespace LabTrack.Application.Services;

public class AssetService : IAssetService
{
    private readonly IUnitOfWork _unitOfWork;

    public AssetService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AssetDto?> GetByIdAsync(
        Guid id, 
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Asset ID cannot be empty.", nameof(id));
        }

        return await Task.FromResult<AssetDto?>(null);
    }

    public async Task<AssetDto?> GetByAssetTagAsync(
        string assetTag, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(assetTag))
        {
            throw new ArgumentException("Asset tag cannot be empty.", nameof(assetTag));
        }

        return await Task.FromResult<AssetDto?>(null);
    }

    public async Task<AssetDto?> GetBySerialNumberAsync(
        string serialNumber, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(serialNumber))
        {
            throw new ArgumentException("Serial number cannot be empty.", nameof(serialNumber));
        }

        return await Task.FromResult<AssetDto?>(null);
    }

    public async Task<IReadOnlyList<AssetDto>> GetByLaboratoryIdAsync(
        Guid laboratoryId, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default)
    {
        if (laboratoryId == Guid.Empty)
        {
            throw new ArgumentException("Laboratory ID cannot be empty.", nameof(laboratoryId));
        }

        return await Task.FromResult<IReadOnlyList<AssetDto>>(Array.Empty<AssetDto>());
    }

    public async Task<IReadOnlyList<AssetDto>> GetByWorkstationIdAsync(
        Guid workstationId, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default)
    {
        if (workstationId == Guid.Empty)
        {
            throw new ArgumentException("Workstation ID cannot be empty.", nameof(workstationId));
        }

        return await Task.FromResult<IReadOnlyList<AssetDto>>(Array.Empty<AssetDto>());
    }

    public async Task<IReadOnlyList<AssetDto>> GetByStatusAsync(
        string status, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            throw new ArgumentException("Status cannot be empty.", nameof(status));
        }

        return await Task.FromResult<IReadOnlyList<AssetDto>>(Array.Empty<AssetDto>());
    }

    public async Task<AssetDto> CreateAsync(
        CreateAssetDto dto, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (!await IsAssetTagUniqueAsync(dto.AssetTag, null, cancellationToken))
        {
            throw new InvalidOperationException($"Asset tag '{dto.AssetTag}' is already in use.");
        }

        return await Task.FromResult(new AssetDto
        {
            Id = Guid.NewGuid(),
            AssetTag = dto.AssetTag,
            Name = dto.Name,
            Category = dto.Category,
            SerialNumber = dto.SerialNumber,
            Status = dto.Status,
            LaboratoryId = dto.LaboratoryId,
            WorkstationId = dto.WorkstationId
        });
    }

    public async Task<AssetDto> UpdateAsync(
        Guid id, 
        UpdateAssetDto dto, 
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Asset ID cannot be empty.", nameof(id));
        }

        ArgumentNullException.ThrowIfNull(dto);

      
        if (!string.IsNullOrWhiteSpace(dto.AssetTag) &&
            !await IsAssetTagUniqueAsync(dto.AssetTag, id, cancellationToken))
        {
            throw new InvalidOperationException($"Asset tag '{dto.AssetTag}' is already in use by another asset.");
        }

        return await Task.FromResult(new AssetDto
        {
            Id = id,
            AssetTag = dto.AssetTag ?? string.Empty,
            Name = dto.Name,
            Category = dto.Category,
            SerialNumber = dto.SerialNumber,
            Status = dto.Status,
            LaboratoryId = dto.LaboratoryId,
            WorkstationId = dto.WorkstationId
        });
    }

    public async Task UpdateStatusAsync(
        Guid id, 
        string status, 
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Asset ID cannot be empty.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(status))
        {
            throw new ArgumentException("Status cannot be empty.", nameof(status));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task AssignToWorkstationAsync(
        Guid assetId, 
        Guid workstationId, 
        CancellationToken cancellationToken = default)
    {
        if (assetId == Guid.Empty || workstationId == Guid.Empty)
        {
            throw new ArgumentException("Asset ID and Workstation ID must be valid.");
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        Guid id, 
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Asset ID cannot be empty.", nameof(id));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> IsAssetTagUniqueAsync(
        string assetTag, 
        Guid? excludeAssetId = null, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(assetTag))
        {
            return false;
        }

        return await Task.FromResult(true);
    }
    
}