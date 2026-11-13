using LabTrack.Application.Interfaces.Repositories;
using LabTrack.Domain.Entities;
using LabTrack.Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace LabTrack.Infrastructure.Persistence.Repositories;

public class FinancialRepository : IFinancialRepository
{
    private readonly LabTrackDbContext _context;

    public FinancialRepository(LabTrackDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    // ─── IGenericRepository<FinancialExpense> ───

    public async Task<FinancialExpense?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.FinancialExpenses
            .Include(e => e.Asset)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<FinancialExpense>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.FinancialExpenses
            .Include(e => e.Asset)
            .OrderByDescending(e => e.ExpenseDateUtc)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<FinancialExpense> AddAsync(FinancialExpense entity, CancellationToken cancellationToken = default)
    {
        await _context.FinancialExpenses.AddAsync(entity, cancellationToken);
        return entity;
    }

    public Task UpdateAsync(FinancialExpense entity, CancellationToken cancellationToken = default)
    {
        _context.FinancialExpenses.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(FinancialExpense entity, CancellationToken cancellationToken = default)
    {
        _context.FinancialExpenses.Remove(entity);
        return Task.CompletedTask;
    }

    // ─── IFinancialRepository specific ───

    public async Task<IReadOnlyList<FinancialExpense>> GetByAssetIdAsync(
        Guid assetId,
        CancellationToken cancellationToken = default)
    {
        return await _context.FinancialExpenses
            .Where(e => e.AssetId == assetId)
            .OrderByDescending(e => e.ExpenseDateUtc)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FinancialExpense>> GetByLaboratoryIdAsync(
        Guid laboratoryId,
        CancellationToken cancellationToken = default)
    {
        return await _context.FinancialExpenses
            .Where(e => e.LaboratoryId == laboratoryId)
            .OrderByDescending(e => e.ExpenseDateUtc)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FinancialExpense>> GetByRepairTicketIdAsync(
        Guid repairTicketId,
        CancellationToken cancellationToken = default)
    {
        // FinancialExpense has no RepairTicketId — return empty
        return await Task.FromResult<IReadOnlyList<FinancialExpense>>(Array.Empty<FinancialExpense>());
    }

    public async Task<IReadOnlyList<FinancialExpense>> GetByTransactionTypeAsync(
        string transactionType,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        // FinancialExpense uses "Category" as transaction type equivalent
        return await _context.FinancialExpenses
            .Where(e => e.Category == transactionType)
            .OrderByDescending(e => e.ExpenseDateUtc)
            .Skip(skip)
            .Take(take)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FinancialExpense>> GetByDateRangeAsync(
        DateTime startUtc,
        DateTime endUtc,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        return await _context.FinancialExpenses
            .Where(e => e.ExpenseDateUtc >= startUtc && e.ExpenseDateUtc <= endUtc)
            .OrderByDescending(e => e.ExpenseDateUtc)
            .Skip(skip)
            .Take(take)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal> GetTotalExpensesByLaboratoryAsync(
        Guid laboratoryId,
        DateTime? startUtc = null,
        DateTime? endUtc = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.FinancialExpenses
            .Where(e => e.LaboratoryId == laboratoryId);

        if (startUtc.HasValue)
            query = query.Where(e => e.ExpenseDateUtc >= startUtc.Value);

        if (endUtc.HasValue)
            query = query.Where(e => e.ExpenseDateUtc <= endUtc.Value);

        return await query.SumAsync(e => e.Amount, cancellationToken);
    }
}