using LabTrack.Application.Dtos.Maintenance;

namespace LabTrack.Application.Interfaces;

public interface IMaintenanceService
{
    Task<MaintenanceScheduleDto?> GetByIdAsync(
        Guid id, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MaintenanceScheduleDto>> GetByLaboratoryIdAsync(
        Guid laboratoryId, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MaintenanceScheduleDto>> GetByAssetIdAsync(
        Guid assetId, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MaintenanceScheduleDto>> GetUpcomingSchedulesAsync(
        Guid? laboratoryId = null, 
        int daysAhead = 7, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MaintenanceScheduleDto>> GetOverdueSchedulesAsync(
        Guid? laboratoryId = null, 
        CancellationToken cancellationToken = default);

    Task<MaintenanceScheduleDto> CreateScheduleAsync(
        CreateMaintenanceScheduleDto dto, 
        CancellationToken cancellationToken = default);

    Task<MaintenanceScheduleDto> UpdateScheduleAsync(
        Guid id, 
        UpdateMaintenanceScheduleDto dto, 
        CancellationToken cancellationToken = default);

    Task CompleteScheduleAsync(
        Guid id, 
        string technicianNotes, 
        CancellationToken cancellationToken = default);

    Task CancelScheduleAsync(
        Guid id, 
        string reason, 
        CancellationToken cancellationToken = default);

}