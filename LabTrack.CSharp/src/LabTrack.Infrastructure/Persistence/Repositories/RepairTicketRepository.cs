using LabTrack.Domain;
using LabTrack.Domain.Entities;
using LabTrack.Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace LabTrack.Infrastructure.Persistence.Repositories;

public class RepairTicketRepository : IRepairTicketRepository
{
    private readonly LabTrackDbContext _context;

    public RepairTicketRepository(LabTrackDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<RepairTicket?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.RepairTickets
            .Include(t => t.Asset)
            .Include(t => t.ReportedByUser)
            .Include("AssignedTechnician")
            .Include("FinancialExpenses")
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<RepairTicket>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.RepairTickets
            .Include(t => t.Asset)
            .Include(t => t.ReportedByUser)
            .Include("AssignedTechnician")
            .OrderByDescending(t => EF.Property<DateTime>(t, "CreatedAt"))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<RepairTicket> AddAsync(RepairTicket ticket, CancellationToken cancellationToken = default)
    {
        await _context.RepairTickets.AddAsync(ticket, cancellationToken);
        return ticket;
    }

    public Task UpdateAsync(RepairTicket ticket, CancellationToken cancellationToken = default)
    {
        _context.RepairTickets.Update(ticket);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(RepairTicket ticket, CancellationToken cancellationToken = default)
    {
        _context.RepairTickets.Remove(ticket);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<RepairTicket>> GetByAssetIdAsync(Guid assetId, CancellationToken cancellationToken = default)
    {
        return await _context.RepairTickets
            .Include(t => t.ReportedByUser)
            .Include("AssignedTechnician")
            .Where(t => EF.Property<Guid>(t, "AssetId") == assetId)
            .OrderByDescending(t => EF.Property<DateTime>(t, "CreatedAt"))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<RepairTicket>> GetByReportedUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.RepairTickets
            .Include(t => t.Asset)
            .Include("AssignedTechnician")
            .Where(t => EF.Property<Guid>(t, "ReportedByUserId") == userId)
            .OrderByDescending(t => EF.Property<DateTime>(t, "CreatedAt"))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RepairTicket>> GetByAssignedTechnicianIdAsync(Guid technicianId, CancellationToken cancellationToken = default)
    {
        return await _context.RepairTickets
            .Include(t => t.Asset)
            .Include(t => t.ReportedByUser)
            .Where(t => EF.Property<Guid?>(t, "AssignedTechnicianId") == technicianId)
            .OrderByDescending(t => EF.Property<DateTime>(t, "CreatedAt"))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RepairTicket>> GetByStatusAsync(string status, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;

        return await _context.RepairTickets
            .Include(t => t.Asset)
            .Include(t => t.ReportedByUser)
            .Include("AssignedTechnician")
            .Where(t => t.Status == status)
            .OrderByDescending(t => EF.Property<DateTime>(t, "CreatedAt"))
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<RepairTicket?> GetByTicketNumberAsync(string ticketNumber, CancellationToken cancellationToken = default)
    {
        return await _context.RepairTickets
            .Include(t => t.Asset)
            .Include(t => t.ReportedByUser)
            .Include("AssignedTechnician")
            .Include("FinancialExpenses")
            .FirstOrDefaultAsync(t => EF.Property<string>(t, "TicketNumber") == ticketNumber, cancellationToken);
    }

    public async Task<IReadOnlyList<RepairTicket>> GetByWorkstationIdAsync(Guid workstationId, CancellationToken cancellationToken = default)
    {
        return await _context.RepairTickets
            .Include(t => t.Asset)
            .Include(t => t.ReportedByUser)
            .Include("AssignedTechnician")
            .Where(t => t.Asset != null && EF.Property<Guid?>(t.Asset, "WorkstationId") == workstationId)
            .OrderByDescending(t => EF.Property<DateTime>(t, "CreatedAt"))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RepairTicket>> GetByLaboratoryIdAsync(Guid laboratoryId, CancellationToken cancellationToken = default)
    {
        return await _context.RepairTickets
            .Include(t => t.Asset)
            .Include(t => t.ReportedByUser)
            .Include("AssignedTechnician")
            .Where(t => t.Asset != null && EF.Property<Guid?>(t.Asset, "LaboratoryId") == laboratoryId)
            .OrderByDescending(t => EF.Property<DateTime>(t, "CreatedAt"))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RepairTicket>> GetByProblemReportIdAsync(Guid problemReportId, CancellationToken cancellationToken = default)
    {
        return await _context.RepairTickets
            .Include(t => t.Asset)
            .Include(t => t.ReportedByUser)
            .Include("AssignedTechnician")
            .Where(t => EF.Property<Guid?>(t, "ProblemReportId") == problemReportId)
            .OrderByDescending(t => EF.Property<DateTime>(t, "CreatedAt"))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsTicketNumberUniqueAsync(string ticketNumber, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.RepairTickets
            .Where(t => EF.Property<string>(t, "TicketNumber") == ticketNumber);

        if (excludeId.HasValue)
            query = query.Where(t => t.Id != excludeId.Value);

        return !await query.AnyAsync(cancellationToken);
    }
}