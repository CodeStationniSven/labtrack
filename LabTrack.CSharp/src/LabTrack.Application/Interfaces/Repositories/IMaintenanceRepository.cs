using LabTrack.Domain.Entities;

namespace LabTrack.Application.Interfaces.Repositories;

public interface IMaintenanceRepository : IGenericRepository<MaintenanceSchedule>
{
    Task<IReadOnlyList<MaintenanceSchedule>> GetByLaboratoryIdAsync(
        Guid laboratoryId, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MaintenanceSchedule>> GetByWorkstationIdAsync(
        Guid workstationId, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MaintenanceSchedule>> GetByAssetIdAsync(
        Guid assetId, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MaintenanceSchedule>> GetPendingSchedulesAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MaintenanceSchedule>> GetSchedulesDueByDateAsync(
        DateTime dueBeforeUtc, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MaintenanceSchedule>> GetByTechnicianIdAsync(
        Guid technicianId, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MaintenanceSchedule>> GetByStatusAsync(
        string status, 
        CancellationToken cancellationToken = default);
}