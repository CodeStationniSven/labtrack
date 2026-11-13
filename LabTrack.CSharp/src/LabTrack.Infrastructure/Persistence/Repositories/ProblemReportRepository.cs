using LabTrack.Application.Interfaces.Repositories;
using LabTrack.Domain.Entities;
using LabTrack.Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace LabTrack.Infrastructure.Persistence.Repositories;

public class ProblemReportRepository : IProblemReportRepository
{
    private readonly LabTrackDbContext _context;

    public ProblemReportRepository(LabTrackDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    // ─── IGenericRepository<ProblemReport> ───

    public async Task<ProblemReport?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.ProblemReports
            .Include(r => r.ReportedByUser)
            .Include(r => r.Asset)
            .Include(r => r.Laboratory)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ProblemReport>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.ProblemReports
            .Include(r => r.ReportedByUser)
            .Include(r => r.Asset)
            .Include(r => r.Laboratory)
            .OrderByDescending(r => r.CreatedAtUtc)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<ProblemReport> AddAsync(
        ProblemReport entity,
        CancellationToken cancellationToken = default)
    {
        await _context.ProblemReports.AddAsync(entity, cancellationToken);
        return entity;
    }

    public Task UpdateAsync(
        ProblemReport entity,
        CancellationToken cancellationToken = default)
    {
        _context.ProblemReports.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(
        ProblemReport entity,
        CancellationToken cancellationToken = default)
    {
        _context.ProblemReports.Remove(entity);
        return Task.CompletedTask;
    }

    // ─── IProblemReportRepository specific ───

    public async Task<IReadOnlyList<ProblemReport>> GetByLaboratoryIdAsync(
        Guid laboratoryId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ProblemReports
            .Include(r => r.ReportedByUser)
            .Include(r => r.Asset)
            .Where(r => r.LaboratoryId == laboratoryId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public Task<IReadOnlyList<ProblemReport>> GetByWorkstationIdAsync(
        Guid workstationId,
        CancellationToken cancellationToken = default)
    {
        // ProblemReport has no WorkstationId property — return empty
        return Task.FromResult<IReadOnlyList<ProblemReport>>(Array.Empty<ProblemReport>());
    }

    public async Task<IReadOnlyList<ProblemReport>> GetByReporterIdAsync(
        Guid reporterId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ProblemReports
            .Include(r => r.Asset)
            .Include(r => r.Laboratory)
            .Where(r => r.ReportedByUserId == reporterId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProblemReport>> GetByStatusAsync(
        string status,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        return await _context.ProblemReports
            .Include(r => r.ReportedByUser)
            .Include(r => r.Asset)
            .Where(r => r.Status == status)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProblemReport>> GetBySeverityAsync(
        string severity,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        // ProblemReport has "Priority" not "Severity" — map severity to priority
        return await _context.ProblemReports
            .Include(r => r.ReportedByUser)
            .Include(r => r.Asset)
            .Where(r => r.Priority == severity)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProblemReport>> GetUnresolvedReportsAsync(
        Guid? laboratoryId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ProblemReports
            .Include(r => r.ReportedByUser)
            .Include(r => r.Asset)
            .Where(r => r.Status != "Resolved" && r.Status != "Closed");

        if (laboratoryId.HasValue)
        {
            query = query.Where(r => r.LaboratoryId == laboratoryId.Value);
        }

        return await query
            .OrderByDescending(r => r.CreatedAtUtc)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    // ─── Non-interface helper ───

    public async Task<IReadOnlyList<ProblemReport>> GetByAssetIdAsync(
        Guid assetId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ProblemReports
            .Include(r => r.ReportedByUser)
            .Where(r => r.AssetId == assetId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}