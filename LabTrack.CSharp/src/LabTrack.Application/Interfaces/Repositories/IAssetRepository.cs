using LabTrack.Domain.Entities;

namespace LabTrack.Application.Interfaces.Repositories;

public interface IAssetRepository : IGenericRepository<Asset>
{
    Task<Asset?> GetBySerialNumberAsync(
        string serialNumber, 
        CancellationToken cancellationToken = default);

    Task<Asset?> GetByAssetTagAsync(
        string assetTag, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Asset>> GetByLaboratoryIdAsync(
        Guid laboratoryId, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Asset>> GetByWorkstationIdAsync(
        Guid workstationId, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Asset>> GetByStatusAsync(
        string status, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Asset>> GetByCategoryAsync(
        string category, 
        CancellationToken cancellationToken = default);

    Task<bool> IsAssetTagUniqueAsync(
        string assetTag, 
        Guid? excludeAssetId = null, 
        CancellationToken cancellationToken = default);

    Task<bool> IsSerialNumberUniqueAsync(
        string serialNumber, 
        Guid? excludeAssetId = null, 
        CancellationToken cancellationToken = default);
}