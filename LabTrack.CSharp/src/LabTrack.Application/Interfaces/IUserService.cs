using LabTrack.Application.Dtos.Users;

namespace LabTrack.Application.Interfaces;

public interface IUserService
{
    Task<UserDto?> GetByIdAsync(
        Guid id, 
        CancellationToken cancellationToken = default);

    Task<UserDto?> GetByEmailAsync(
        string email, 
        CancellationToken cancellationToken = default);

    Task<UserDto?> GetByUsernameAsync(
        string username, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserDto>> GetAllAsync(
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserDto>> GetByRoleAsync(
        string role, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default);

    Task<UserDto> CreateAsync(
        CreateUserDto dto, 
        CancellationToken cancellationToken = default);

    Task<UserDto> UpdateAsync(
        Guid id, 
        UpdateUserDto dto, 
        CancellationToken cancellationToken = default);

    Task ChangePasswordAsync(
        Guid id, 
        ChangePasswordDto dto, 
        CancellationToken cancellationToken = default);

    Task AssignRolesAsync(
        Guid id, 
        IEnumerable<string> roles, 
        CancellationToken cancellationToken = default);

    Task DeactivateAsync(
        Guid id, 
        CancellationToken cancellationToken = default);

    Task ActivateAsync(
        Guid id, 
        CancellationToken cancellationToken = default);

    Task<bool> IsEmailUniqueAsync(
        string email, 
        Guid? excludeUserId = null, 
        CancellationToken cancellationToken = default);

    Task<bool> IsUsernameUniqueAsync(
        string username, 
        Guid? excludeUserId = null, 
        CancellationToken cancellationToken = default);

}