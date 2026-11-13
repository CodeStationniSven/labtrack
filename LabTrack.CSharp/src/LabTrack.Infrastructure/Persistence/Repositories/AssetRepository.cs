using LabTrack.Application.Interfaces.Repositories;
using LabTrack.Domain.Entities;
using LabTrack.Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace LabTrack.Infrastructure.Persistence.Repositories;

public class AssetRepository : IAssetRepository
{
    private readonly LabTrackDbContext _context;

    public AssetRepository(LabTrackDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    // ─── IGenericRepository<Asset> ───

    public async Task<Asset?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Assets.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Asset>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Assets
            .OrderByDescending(a => a.CreatedAtUtc)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Asset> AddAsync(Asset entity, CancellationToken cancellationToken = default)
    {
        await _context.Assets.AddAsync(entity, cancellationToken);
        return entity;
    }

    public Task UpdateAsync(Asset entity, CancellationToken cancellationToken = default)
    {
        _context.Assets.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Asset entity, CancellationToken cancellationToken = default)
    {
        _context.Assets.Remove(entity);
        return Task.CompletedTask;
    }

    // ─── IAssetRepository specific ───

    public async Task<Asset?> GetByAssetTagAsync(
        string assetTag,
        CancellationToken cancellationToken = default)
    {
        return await _context.Assets
            .FirstOrDefaultAsync(a => a.AssetTag == assetTag, cancellationToken);
    }

    public async Task<Asset?> GetBySerialNumberAsync(
        string serialNumber,
        CancellationToken cancellationToken = default)
    {
        return await _context.Assets
            .FirstOrDefaultAsync(a => a.SerialNumber == serialNumber, cancellationToken);
    }

    public async Task<IReadOnlyList<Asset>> GetByLaboratoryIdAsync(
        Guid laboratoryId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Assets
            .Where(a => a.LaboratoryId == laboratoryId)
            .OrderByDescending(a => a.CreatedAtUtc)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public Task<IReadOnlyList<Asset>> GetByWorkstationIdAsync(
        Guid workstationId,
        CancellationToken cancellationToken = default)
    {
        // Asset entity has no WorkstationId property — return empty list
        return Task.FromResult<IReadOnlyList<Asset>>(Array.Empty<Asset>());
    }

    public async Task<IReadOnlyList<Asset>> GetByStatusAsync(
        string status,
        CancellationToken cancellationToken = default)
    {
        return await _context.Assets
            .Where(a => a.Status == status)
            .OrderByDescending(a => a.CreatedAtUtc)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Asset>> GetByCategoryAsync(
        string category,
        CancellationToken cancellationToken = default)
    {
        return await _context.Assets
            .Where(a => a.Category == category)
            .OrderByDescending(a => a.CreatedAtUtc)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsAssetTagUniqueAsync(
        string assetTag,
        Guid? excludeAssetId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(assetTag))
            return false;

        var query = _context.Assets.Where(a => a.AssetTag == assetTag);

        if (excludeAssetId.HasValue)
            query = query.Where(a => a.Id != excludeAssetId.Value);

        return !await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> IsSerialNumberUniqueAsync(
        string serialNumber,
        Guid? excludeAssetId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(serialNumber))
            return false;

        var query = _context.Assets.Where(a => a.SerialNumber == serialNumber);

        if (excludeAssetId.HasValue)
            query = query.Where(a => a.Id != excludeAssetId.Value);

        return !await query.AnyAsync(cancellationToken);
    }
}