using LabTrack.Application.Dtos.Users;
using LabTrack.Application.Interfaces;

namespace LabTrack.Application.Services;

public class UserService : IUserService
{
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public UserService(
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<UserDto?> GetByIdAsync(
        Guid id, 
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty.", nameof(id));
        }

        return await Task.FromResult<UserDto?>(null);
    }

    public async Task<UserDto?> GetByEmailAsync(
        string email, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email cannot be empty.", nameof(email));
        }

        return await Task.FromResult<UserDto?>(null);
    }

    public async Task<UserDto?> GetByUsernameAsync(
        string username, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Username cannot be empty.", nameof(username));
        }

        return await Task.FromResult<UserDto?>(null);
    }

    public async Task<IReadOnlyList<UserDto>> GetAllAsync(
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default)
    {
        return await Task.FromResult<IReadOnlyList<UserDto>>(Array.Empty<UserDto>());
    }

    public async Task<IReadOnlyList<UserDto>> GetByRoleAsync(
        string role, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            throw new ArgumentException("Role cannot be empty.", nameof(role));
        }

        return await Task.FromResult<IReadOnlyList<UserDto>>(Array.Empty<UserDto>());
    }

    public async Task<UserDto> CreateAsync(
        CreateUserDto dto, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (!await IsEmailUniqueAsync(dto.Email, null, cancellationToken))
        {
            throw new InvalidOperationException($"Email '{dto.Email}' is already registered.");
        }

        if (!await IsUsernameUniqueAsync(dto.Username, null, cancellationToken))
        {
            throw new InvalidOperationException($"Username '{dto.Username}' is already taken.");
        }

        var passwordHash = _passwordHasher.HashPassword(dto.Password);

        return await Task.FromResult(new UserDto
        {
            Id = Guid.NewGuid(),
            Username = dto.Username,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            IsActive = true,
            Roles = dto.Roles ?? new List<string>()
        });
    }

    public async Task<UserDto> UpdateAsync(
        Guid id, 
        UpdateUserDto dto, 
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty.", nameof(id));
        }

        ArgumentNullException.ThrowIfNull(dto);

        if (!await IsEmailUniqueAsync(dto.Email, id, cancellationToken))
        {
            throw new InvalidOperationException($"Email '{dto.Email}' is already in use by another user.");
        }

        if (!await IsUsernameUniqueAsync(dto.Username, id, cancellationToken))
        {
            throw new InvalidOperationException($"Username '{dto.Username}' is already in use by another user.");
        }

        return await Task.FromResult(new UserDto
        {
            Id = id,
            Username = dto.Username,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            IsActive = dto.IsActive
        });
    }

    public async Task ChangePasswordAsync(
        Guid id, 
        ChangePasswordDto dto, 
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty.", nameof(id));
        }

        ArgumentNullException.ThrowIfNull(dto);

        if (string.IsNullOrWhiteSpace(dto.NewPassword))
        {
            throw new ArgumentException("New password cannot be empty.", nameof(dto.NewPassword));
        }

        var newHash = _passwordHasher.HashPassword(dto.NewPassword);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task AssignRolesAsync(
        Guid id, 
        IEnumerable<string> roles, 
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty.", nameof(id));
        }

        ArgumentNullException.ThrowIfNull(roles);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateAsync(
        Guid id, 
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty.", nameof(id));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ActivateAsync(
        Guid id, 
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty.", nameof(id));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> IsEmailUniqueAsync(
        string email, 
        Guid? excludeUserId = null, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        return await Task.FromResult(true);
    }

    public async Task<bool> IsUsernameUniqueAsync(
        string username, 
        Guid? excludeUserId = null, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return false;
        }

        return await Task.FromResult(true);
    }
    
}