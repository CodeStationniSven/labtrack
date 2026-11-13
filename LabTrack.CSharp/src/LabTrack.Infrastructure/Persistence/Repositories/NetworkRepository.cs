using LabTrack.Application.Interfaces.Repositories;
using LabTrack.Domain.Entities;
using LabTrack.Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace LabTrack.Infrastructure.Persistence.Repositories;

public class NetworkRepository : INetworkRepository
{
    private readonly LabTrackDbContext _context;

    public NetworkRepository(LabTrackDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    // ─── IGenericRepository<NetworkDevice> ───

    public async Task<NetworkDevice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<NetworkDevice>()
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<NetworkDevice>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<NetworkDevice>()
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<NetworkDevice> AddAsync(NetworkDevice entity, CancellationToken cancellationToken = default)
    {
        await _context.Set<NetworkDevice>().AddAsync(entity, cancellationToken);
        return entity;
    }

    public Task UpdateAsync(NetworkDevice entity, CancellationToken cancellationToken = default)
    {
        _context.Set<NetworkDevice>().Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(NetworkDevice entity, CancellationToken cancellationToken = default)
    {
        _context.Set<NetworkDevice>().Remove(entity);
        return Task.CompletedTask;
    }

    // ─── INetworkRepository specific ───

    public async Task<NetworkDevice?> GetByIpAddressAsync(
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<NetworkDevice>()
            .FirstOrDefaultAsync(d => d.IpAddress == ipAddress, cancellationToken);
    }

    public async Task<NetworkDevice?> GetByMacAddressAsync(
        string macAddress,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<NetworkDevice>()
            .FirstOrDefaultAsync(d => d.MacAddress == macAddress, cancellationToken);
    }

    public Task<IReadOnlyList<NetworkDevice>> GetByLaboratoryIdAsync(
        Guid laboratoryId,
        CancellationToken cancellationToken = default)
    {
        // NetworkDevice has no LaboratoryId property — return empty
        return Task.FromResult<IReadOnlyList<NetworkDevice>>(Array.Empty<NetworkDevice>());
    }

    public Task<IReadOnlyList<NetworkDevice>> GetByWorkstationIdAsync(
        Guid workstationId,
        CancellationToken cancellationToken = default)
    {
        // NetworkDevice has no WorkstationId property — return empty
        return Task.FromResult<IReadOnlyList<NetworkDevice>>(Array.Empty<NetworkDevice>());
    }

    public async Task<IReadOnlyList<NetworkDevice>> GetByStatusAsync(
        string status,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<NetworkDevice>()
            .Where(d => d.Status == status)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsIpAddressUniqueAsync(
        string ipAddress,
        Guid? excludeDeviceId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return false;

        var query = _context.Set<NetworkDevice>()
            .Where(d => d.IpAddress == ipAddress);

        if (excludeDeviceId.HasValue)
            query = query.Where(d => d.Id != excludeDeviceId.Value);

        return !await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> IsMacAddressUniqueAsync(
        string macAddress,
        Guid? excludeDeviceId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(macAddress))
            return false;

        var query = _context.Set<NetworkDevice>()
            .Where(d => d.MacAddress == macAddress);

        if (excludeDeviceId.HasValue)
            query = query.Where(d => d.Id != excludeDeviceId.Value);

        return !await query.AnyAsync(cancellationToken);
    }
}