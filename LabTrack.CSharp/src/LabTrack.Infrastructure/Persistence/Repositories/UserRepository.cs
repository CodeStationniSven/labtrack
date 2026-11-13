using LabTrack.Application.Interfaces.Repositories;
using LabTrack.Domain.Entities;
using LabTrack.Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace LabTrack.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    
    private const string RoleField         = "Role";
    private const string IsActiveField     = "IsActive";
    private const string EmailField        = "Email";
    private const string SchoolIdField     = "SchoolId";
    private const string LaboratoryIdField = "LaboratoryId";

    private readonly LabTrackDbContext _context;

    public UserRepository(LabTrackDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }


    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include("AuthSessions")
            .Include("Notifications")
            .Include("Reservations")
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<User> AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
        return user;
    }

    public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Update(user);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Remove(user);
        return Task.CompletedTask;
    }

   

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var lowered = email.ToLower();
        return await _context.Users
            .Include("AuthSessions")
            .FirstOrDefaultAsync(
                u => EF.Property<string>(u, EmailField).ToLower() == lowered,
                cancellationToken);
    }

    public async Task<User?> GetBySchoolIdAsync(string schoolId, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(
                u => EF.Property<string>(u, SchoolIdField) == schoolId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetByRoleAsync(string role, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;

        return await _context.Users
            .Where(u => EF.Property<string>(u, RoleField) == role)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetActiveUsersAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;

        return await _context.Users
            .Where(u => EF.Property<bool>(u, IsActiveField))
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetByLaboratoryIdAsync(Guid laboratoryId, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Where(u => EF.Property<Guid?>(u, LaboratoryIdField) == laboratoryId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsEmailUniqueAsync(string email, Guid? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        var lowered = email.ToLower();

        var query = _context.Users
            .Where(u => EF.Property<string>(u, EmailField).ToLower() == lowered);

        if (excludeUserId.HasValue)
            query = query.Where(u => u.Id != excludeUserId.Value);

        return !await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> IsSchoolIdUniqueAsync(string schoolId, Guid? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Users
            .Where(u => EF.Property<string>(u, SchoolIdField) == schoolId);

        if (excludeUserId.HasValue)
            query = query.Where(u => u.Id != excludeUserId.Value);

        return !await query.AnyAsync(cancellationToken);
    }
}