using LabTrack.Domain.Entities;

namespace LabTrack.Application.Interfaces.Repositories;

public interface IUserRepository : IGenericRepository<User>
{
    Task<User?> GetByEmailAsync(
        string email, 
        CancellationToken cancellationToken = default);

    Task<User?> GetBySchoolIdAsync(
        string schoolId, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<User>> GetByRoleAsync(
        string role, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<User>> GetByLaboratoryIdAsync(
        Guid laboratoryId, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<User>> GetActiveUsersAsync(
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default);

    Task<bool> IsEmailUniqueAsync(
        string email, 
        Guid? excludeUserId = null, 
        CancellationToken cancellationToken = default);

    Task<bool> IsSchoolIdUniqueAsync(
        string schoolId, 
        Guid? excludeUserId = null, 
        CancellationToken cancellationToken = default);
}