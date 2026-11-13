using LabTrack.Application.Interfaces.Repositories;
using LabTrack.Domain.Entities;
using LabTrack.Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace LabTrack.Infrastructure.Persistence.Repositories;

public class LaboratoryRepository : ILaboratoryRepository
{
    private readonly LabTrackDbContext _context;

    public LaboratoryRepository(LabTrackDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    // ─── IGenericRepository<Laboratory> ───

    public async Task<Laboratory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Laboratories
            .Include(l => l.Assets)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Laboratory>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Laboratories
            .OrderBy(l => l.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Laboratory> AddAsync(Laboratory entity, CancellationToken cancellationToken = default)
    {
        await _context.Laboratories.AddAsync(entity, cancellationToken);
        return entity;
    }

    public Task UpdateAsync(Laboratory entity, CancellationToken cancellationToken = default)
    {
        _context.Laboratories.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Laboratory entity, CancellationToken cancellationToken = default)
    {
        _context.Laboratories.Remove(entity);
        return Task.CompletedTask;
    }

    // ─── ILaboratoryRepository specific ───

    public Task<Laboratory?> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        // Laboratory has no Code property — return null
        return Task.FromResult<Laboratory?>(null);
    }

    public async Task<Laboratory?> GetByNameAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        return await _context.Laboratories
            .FirstOrDefaultAsync(l => l.Name == name, cancellationToken);
    }

    public async Task<IReadOnlyList<Laboratory>> GetActiveLaboratoriesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Laboratories
            .Where(l => l.IsActive)
            .OrderBy(l => l.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public Task<IReadOnlyList<Laboratory>> GetByDepartmentAsync(
        string department,
        CancellationToken cancellationToken = default)
    {
        // Laboratory has no Department property — return empty list
        return Task.FromResult<IReadOnlyList<Laboratory>>(Array.Empty<Laboratory>());
    }

    public async Task<Laboratory?> GetWithWorkstationsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        // Laboratory has no Workstations nav — return with Assets instead
        return await _context.Laboratories
            .Include(l => l.Assets)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    public Task<bool> IsCodeUniqueAsync(
        string code,
        Guid? excludeLaboratoryId = null,
        CancellationToken cancellationToken = default)
    {
        // Laboratory has no Code property — always return true
        return Task.FromResult(true);
    }

    public async Task<bool> IsNameUniqueAsync(
        string name,
        Guid? excludeLaboratoryId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        var query = _context.Laboratories.Where(l => l.Name == name);

        if (excludeLaboratoryId.HasValue)
            query = query.Where(l => l.Id != excludeLaboratoryId.Value);

        return !await query.AnyAsync(cancellationToken);
    }
}