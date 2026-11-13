using LabTrack.Application.Interfaces.Repositories;
using LabTrack.Domain.Entities;
using LabTrack.Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace LabTrack.Infrastructure.Persistence.Repositories;

public sealed class PcUsageRepository : IPcUsageRepository
{
    private const string WorkstationIdField = "WorkstationId";
    private const string StudentIdField     = "StudentId";
    private const string LaboratoryIdField  = "LaboratoryId";
    private const string StartTimeField     = "SessionStartUtc";
    private const string EndTimeField     = "SessionEndUtc";
    private const string CreatedAtField     = "SessionStartUtc";

    private readonly LabTrackDbContext _context;

    public PcUsageRepository(LabTrackDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    private DbSet<PcUsageLog> Sessions => _context.Set<PcUsageLog>();

    public async Task<PcUsageLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await Sessions.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<IReadOnlyList<PcUsageLog>> GetAllAsync(CancellationToken cancellationToken = default)
        => await Sessions
            .OrderByDescending(s => EF.Property<DateTime>(s, CreatedAtField))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<PcUsageLog> AddAsync(PcUsageLog session, CancellationToken cancellationToken = default)
    {
        await Sessions.AddAsync(session, cancellationToken);
        return session;
    }

    public Task UpdateAsync(PcUsageLog session, CancellationToken cancellationToken = default)
    {
        Sessions.Update(session);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(PcUsageLog session, CancellationToken cancellationToken = default)
    {
        Sessions.Remove(session);
        return Task.CompletedTask;
    }

    public async Task<PcUsageLog?> GetActiveSessionByWorkstationIdAsync(
        Guid workstationId, CancellationToken cancellationToken = default)
        => await Sessions
            .Where(s => EF.Property<Guid>(s, WorkstationIdField) == workstationId
                     && EF.Property<DateTime?>(s, EndTimeField) == null)
            .OrderByDescending(s => EF.Property<DateTime>(s, StartTimeField))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<PcUsageLog?> GetActiveSessionByStudentIdAsync(
        Guid studentId, CancellationToken cancellationToken = default)
        => await Sessions
            .Where(s => EF.Property<Guid>(s, StudentIdField) == studentId
                     && EF.Property<DateTime?>(s, EndTimeField) == null)
            .OrderByDescending(s => EF.Property<DateTime>(s, StartTimeField))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<PcUsageLog>> GetActiveSessionsByLaboratoryIdAsync(
        Guid laboratoryId, CancellationToken cancellationToken = default)
        => await Sessions
            .Where(s => EF.Property<Guid>(s, LaboratoryIdField) == laboratoryId
                     && EF.Property<DateTime?>(s, EndTimeField) == null)
            .OrderByDescending(s => EF.Property<DateTime>(s, StartTimeField))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PcUsageLog>> GetSessionHistoryByStudentIdAsync(
        Guid studentId, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        if (skip < 0) skip = 0;
        if (take < 1) take = 50;

        return await Sessions
            .Where(s => EF.Property<Guid>(s, StudentIdField) == studentId)
            .OrderByDescending(s => EF.Property<DateTime>(s, StartTimeField))
            .Skip(skip)
            .Take(take)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PcUsageLog>> GetSessionsByDateRangeAsync(
        Guid laboratoryId, DateTime startUtc, DateTime endUtc,
        CancellationToken cancellationToken = default)
        => await Sessions
            .Where(s => EF.Property<Guid>(s, LaboratoryIdField) == laboratoryId
                     && EF.Property<DateTime>(s, StartTimeField) >= startUtc
                     && EF.Property<DateTime>(s, StartTimeField) <= endUtc)
            .OrderByDescending(s => EF.Property<DateTime>(s, StartTimeField))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<bool> HasActiveSessionAsync(
        Guid studentId, CancellationToken cancellationToken = default)
        => await Sessions
            .AnyAsync(s => EF.Property<Guid>(s, StudentIdField) == studentId
                        && EF.Property<DateTime?>(s, EndTimeField) == null,
                      cancellationToken);
}

