using LabTrack.Application.Interfaces.Repositories;
using LabTrack.Domain.Entities;
using LabTrack.Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace LabTrack.Infrastructure.Persistence.Repositories;

public class MaintenanceRepository : IMaintenanceRepository
{
    private readonly LabTrackDbContext _context;

    public MaintenanceRepository(LabTrackDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    // ═══════════════════════════════════════════════════════════
    // IGenericRepository<MaintenanceSchedule>
    // ═══════════════════════════════════════════════════════════

    public async Task<MaintenanceSchedule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.MaintenanceSchedules
            .Include(s => s.Asset)
            .Include(s => s.AssignedToUser)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<MaintenanceSchedule>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.MaintenanceSchedules
            .Include(s => s.Asset)
            .OrderBy(s => s.Title)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<MaintenanceSchedule> AddAsync(MaintenanceSchedule entity, CancellationToken cancellationToken = default)
    {
        await _context.MaintenanceSchedules.AddAsync(entity, cancellationToken);
        return entity;
    }

    public Task UpdateAsync(MaintenanceSchedule entity, CancellationToken cancellationToken = default)
    {
        _context.MaintenanceSchedules.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(MaintenanceSchedule entity, CancellationToken cancellationToken = default)
    {
        _context.MaintenanceSchedules.Remove(entity);
        return Task.CompletedTask;
    }

    // ═══════════════════════════════════════════════════════════
    // IMaintenanceRepository specific
    // ═══════════════════════════════════════════════════════════

    public async Task<IReadOnlyList<MaintenanceSchedule>> GetByAssetIdAsync(
        Guid assetId,
        CancellationToken cancellationToken = default)
    {
        return await _context.MaintenanceSchedules
            .Include(s => s.AssignedToUser)
            .Where(s => s.AssetId == assetId)
            .OrderBy(s => s.Title)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public Task<IReadOnlyList<MaintenanceSchedule>> GetByLaboratoryIdAsync(
        Guid laboratoryId,
        CancellationToken cancellationToken = default)
    {
        // MaintenanceSchedule has no LaboratoryId property — return empty
        return Task.FromResult<IReadOnlyList<MaintenanceSchedule>>(Array.Empty<MaintenanceSchedule>());
    }

    public Task<IReadOnlyList<MaintenanceSchedule>> GetByWorkstationIdAsync(
        Guid workstationId,
        CancellationToken cancellationToken = default)
    {
        // MaintenanceSchedule has no WorkstationId property — return empty
        return Task.FromResult<IReadOnlyList<MaintenanceSchedule>>(Array.Empty<MaintenanceSchedule>());
    }

    public Task<IReadOnlyList<MaintenanceSchedule>> GetPendingSchedulesAsync(
        CancellationToken cancellationToken = default)
    {
        // MaintenanceSchedule has no Status/NextDueDate — return empty
        return Task.FromResult<IReadOnlyList<MaintenanceSchedule>>(Array.Empty<MaintenanceSchedule>());
    }

    public Task<IReadOnlyList<MaintenanceSchedule>> GetSchedulesDueByDateAsync(
        DateTime dueByDate,
        CancellationToken cancellationToken = default)
    {
        // MaintenanceSchedule has no NextDueDate property — return empty
        return Task.FromResult<IReadOnlyList<MaintenanceSchedule>>(Array.Empty<MaintenanceSchedule>());
    }

    public async Task<IReadOnlyList<MaintenanceSchedule>> GetByTechnicianIdAsync(
        Guid technicianId,
        CancellationToken cancellationToken = default)
    {
        return await _context.MaintenanceSchedules
            .Include(s => s.Asset)
            .Where(s => s.AssignedToUserId == technicianId)
            .OrderBy(s => s.Title)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public Task<IReadOnlyList<MaintenanceSchedule>> GetByStatusAsync(
        string status,
        CancellationToken cancellationToken = default)
    {
        // MaintenanceSchedule has no Status property — return empty
        return Task.FromResult<IReadOnlyList<MaintenanceSchedule>>(Array.Empty<MaintenanceSchedule>());
    }

    // ═══════════════════════════════════════════════════════════
    // MaintenanceRecord helpers (non-interface methods)
    // ═══════════════════════════════════════════════════════════

    public async Task<MaintenanceRecord?> GetRecordByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.MaintenanceRecords
            .Include(m => m.Asset)
            .Include(m => m.PerformedByUser)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<MaintenanceRecord>> GetRecordsByAssetIdAsync(
        Guid assetId,
        CancellationToken cancellationToken = default)
    {
        return await _context.MaintenanceRecords
            .Include(m => m.PerformedByUser)
            .Where(m => m.AssetId == assetId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MaintenanceRecord>> GetRecordsByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.MaintenanceRecords
            .Include(m => m.Asset)
            .Where(m => m.PerformedByUserId == userId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task AddRecordAsync(MaintenanceRecord record, CancellationToken cancellationToken = default)
    {
        await _context.MaintenanceRecords.AddAsync(record, cancellationToken);
    }

    public void UpdateRecord(MaintenanceRecord record)
    {
        _context.MaintenanceRecords.Update(record);
    }

    public void DeleteRecord(MaintenanceRecord record)
    {
        _context.MaintenanceRecords.Remove(record);
    }
}