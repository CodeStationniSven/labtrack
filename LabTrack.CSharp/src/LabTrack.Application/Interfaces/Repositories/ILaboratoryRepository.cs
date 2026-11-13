using LabTrack.Domain.Entities;

namespace LabTrack.Application.Interfaces.Repositories;

public interface ILaboratoryRepository : IGenericRepository<Laboratory>
{
    Task<Laboratory?> GetByNameAsync(
        string name, 
        CancellationToken cancellationToken = default);

    Task<Laboratory?> GetByCodeAsync(
        string code, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Laboratory>> GetActiveLaboratoriesAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Laboratory>> GetByDepartmentAsync(
        string department, 
        CancellationToken cancellationToken = default);

    Task<Laboratory?> GetWithWorkstationsAsync(
        Guid laboratoryId, 
        CancellationToken cancellationToken = default);

    Task<bool> IsCodeUniqueAsync(
        string code, 
        Guid? excludeLaboratoryId = null, 
        CancellationToken cancellationToken = default);

    Task<bool> IsNameUniqueAsync(
        string name, 
        Guid? excludeLaboratoryId = null, 
        CancellationToken cancellationToken = default);
}