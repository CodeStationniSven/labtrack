using LabTrack.Application.Dtos.Assets;

namespace LabTrack.Application.Interfaces;

public interface IAssetService
{
    Task<AssetDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<AssetDto?> GetBySerialNumberAsync(
        string serialNumber,
        CancellationToken cancellationToken = default);

    Task<AssetDto?> GetByAssetTagAsync(
        string assetTag,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AssetDto>> GetByLaboratoryIdAsync(
        Guid laboratoryId,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AssetDto>> GetByWorkstationIdAsync(
        Guid workstationId,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AssetDto>> GetByStatusAsync(
        string status,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    Task<AssetDto> CreateAsync(
        CreateAssetDto dto,
        CancellationToken cancellationToken = default);

    Task<AssetDto> UpdateAsync(
        Guid id,
        UpdateAssetDto dto,
        CancellationToken cancellationToken = default);

    Task UpdateStatusAsync(
        Guid id,
        string status,
        CancellationToken cancellationToken = default);

    Task AssignToWorkstationAsync(
        Guid assetId,
        Guid workstationId,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<bool> IsAssetTagUniqueAsync(
        string assetTag,
        Guid? excludeAssetId = null,
        CancellationToken cancellationToken = default);

}