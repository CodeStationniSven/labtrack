using LabTrack.Domain.Entities;

namespace LabTrack.Application.Interfaces.Repositories;

public interface IReplacementRepository : IGenericRepository<ComponentReplacement>
{
    Task<IReadOnlyList<ComponentReplacement>> GetByAssetIdAsync(
        Guid assetId, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ComponentReplacement>> GetByWorkstationIdAsync(
        Guid workstationId, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ComponentReplacement>> GetByRepairTicketIdAsync(
        Guid repairTicketId, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ComponentReplacement>> GetByLaboratoryIdAsync(
        Guid laboratoryId, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ComponentReplacement>> GetByTechnicianIdAsync(
        Guid technicianId, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ComponentReplacement>> GetByDateRangeAsync(
        DateTime startUtc, 
        DateTime endUtc, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default);

    Task<decimal> GetTotalReplacementCostByAssetAsync(
        Guid assetId, 
        CancellationToken cancellationToken = default);
}