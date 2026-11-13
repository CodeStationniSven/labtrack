using LabTrack.Application.Dtos.Laboratories;
using LabTrack.Application.Dtos.Workstations;   

namespace LabTrack.Application.Interfaces;

public interface ILaboratoryService
{
    Task<LaboratoryDto?> GetByIdAsync(
        Guid id, 
        CancellationToken cancellationToken = default);

    Task<LaboratoryDto?> GetByCodeAsync(
        string code, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LaboratoryDto>> GetAllAsync(
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkstationDto>> GetWorkstationsByLaboratoryIdAsync(
        Guid laboratoryId, 
        CancellationToken cancellationToken = default);

    Task<LaboratoryDto> CreateAsync(
        CreateLaboratoryDto dto, 
        CancellationToken cancellationToken = default);

    Task<LaboratoryDto> UpdateAsync(
        Guid id, 
        UpdateLaboratoryDto dto, 
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid id, 
        CancellationToken cancellationToken = default);

    Task<bool> IsCodeUniqueAsync(
        string code, 
        Guid? excludeLaboratoryId = null, 
        CancellationToken cancellationToken = default);

}