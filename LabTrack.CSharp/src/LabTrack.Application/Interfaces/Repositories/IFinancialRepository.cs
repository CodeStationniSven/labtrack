using LabTrack.Domain.Entities;

namespace LabTrack.Application.Interfaces.Repositories;

public interface IFinancialRepository : IGenericRepository<FinancialExpense>
{
    Task<IReadOnlyList<FinancialExpense>> GetByAssetIdAsync(
        Guid assetId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FinancialExpense>> GetByLaboratoryIdAsync(
        Guid laboratoryId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FinancialExpense>> GetByTransactionTypeAsync(
        string transactionType,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FinancialExpense>> GetByDateRangeAsync(
        DateTime startUtc,
        DateTime endUtc,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default);

    Task<decimal> GetTotalExpensesByLaboratoryAsync(
        Guid laboratoryId,
        DateTime? startUtc = null,
        DateTime? endUtc = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FinancialExpense>> GetByRepairTicketIdAsync(
        Guid repairTicketId,
        CancellationToken cancellationToken = default);
}