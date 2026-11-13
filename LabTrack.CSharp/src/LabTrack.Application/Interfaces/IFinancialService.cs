using LabTrack.Application.Dtos.Financials;

namespace LabTrack.Application.Interfaces;

public interface IFinancialService
{
    Task<FinancialExpenseDto?> GetByIdAsync(
        Guid id, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FinancialExpenseDto>> GetByLaboratoryIdAsync(
        Guid laboratoryId, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FinancialExpenseDto>> GetByCategoryAsync(
        string category, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FinancialExpenseDto>> GetByDateRangeAsync(
        DateTime startUtc, 
        DateTime endUtc, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default);

    Task<decimal> GetTotalExpenditureAsync(
        Guid? laboratoryId = null, 
        DateTime? startUtc = null, 
        DateTime? endUtc = null, 
        CancellationToken cancellationToken = default);

    Task<FinancialExpenseDto> CreateRecordAsync(
        CreateFinancialExpenseDto dto, 
        CancellationToken cancellationToken = default);

    Task<FinancialExpenseDto> UpdateRecordAsync(
        Guid id, 
        UpdateFinancialExpenseDto dto, 
        CancellationToken cancellationToken = default);

    Task DeleteRecordAsync(
        Guid id, 
        CancellationToken cancellationToken = default);

}