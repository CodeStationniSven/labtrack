using LabTrack.Application.Dtos.Financials;
using LabTrack.Application.Interfaces;

namespace LabTrack.Application.Services;

public class FinancialService : IFinancialService
{
    private readonly IUnitOfWork _unitOfWork;

    public FinancialService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<FinancialExpenseDto?> GetByIdAsync(
        Guid id, 
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Expense ID cannot be empty.", nameof(id));
        }

        return await Task.FromResult<FinancialExpenseDto?>(null);
    }

    public async Task<IReadOnlyList<FinancialExpenseDto>> GetByLaboratoryIdAsync(
        Guid laboratoryId, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default)
    {
        if (laboratoryId == Guid.Empty)
        {
            throw new ArgumentException("Laboratory ID cannot be empty.", nameof(laboratoryId));
        }

        return await Task.FromResult<IReadOnlyList<FinancialExpenseDto>>(Array.Empty<FinancialExpenseDto>());
    }

    public async Task<IReadOnlyList<FinancialExpenseDto>> GetByCategoryAsync(
        string category, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            throw new ArgumentException("Category cannot be empty.", nameof(category));
        }

        return await Task.FromResult<IReadOnlyList<FinancialExpenseDto>>(Array.Empty<FinancialExpenseDto>());
    }

    public async Task<IReadOnlyList<FinancialExpenseDto>> GetByDateRangeAsync(
        DateTime startUtc, 
        DateTime endUtc, 
        int skip = 0, 
        int take = 100, 
        CancellationToken cancellationToken = default)
    {
        if (startUtc > endUtc)
        {
            throw new ArgumentException("Start date must be earlier than or equal to end date.");
        }

        return await Task.FromResult<IReadOnlyList<FinancialExpenseDto>>(Array.Empty<FinancialExpenseDto>());
    }

    public async Task<decimal> GetTotalExpenditureAsync(
        Guid? laboratoryId = null, 
        DateTime? startUtc = null, 
        DateTime? endUtc = null, 
        CancellationToken cancellationToken = default)
    {
        if (startUtc.HasValue && endUtc.HasValue && startUtc > endUtc)
        {
            throw new ArgumentException("Start date must be earlier than or equal to end date.");
        }

        return await Task.FromResult(0.0m);
    }

    public async Task<FinancialExpenseDto> CreateRecordAsync(
        CreateFinancialExpenseDto dto, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (dto.Amount <= 0)
        {
            throw new ArgumentException("Expense amount must be greater than zero.", nameof(dto.Amount));
        }

        if (string.IsNullOrWhiteSpace(dto.Category))
        {
            throw new ArgumentException("Category must be provided.", nameof(dto.Category));
        }

        return await Task.FromResult(new FinancialExpenseDto
        {
            Id = Guid.NewGuid(),
            LaboratoryId = dto.LaboratoryId,
            WorkstationId = dto.WorkstationId,
            AssetId = dto.AssetId,
            Title = dto.Title,
            Amount = dto.Amount,
            Category = dto.Category,
            Description = dto.Description,
            Currency = dto.Currency,
            ExpenseDateUtc = dto.ExpenseDateUtc,
            VendorOrSupplier = dto.VendorOrSupplier,
            InvoiceOrReceiptNumber = dto.InvoiceOrReceiptNumber,
            CreatedByUserId = dto.CreatedByUserId
        });
    }

    public async Task<FinancialExpenseDto> UpdateRecordAsync(
        Guid id, 
        UpdateFinancialExpenseDto dto, 
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Expense ID cannot be empty.", nameof(id));
        }

        ArgumentNullException.ThrowIfNull(dto);

        if (dto.Amount.HasValue && dto.Amount.Value <= 0)
        {
            throw new ArgumentException("Expense amount must be greater than zero.", nameof(dto.Amount));
        }

        return await Task.FromResult(new FinancialExpenseDto
        {
            Id = id,
            WorkstationId = dto.WorkstationId,
            AssetId = dto.AssetId,
            Title = dto.Title ?? string.Empty,
            Amount = dto.Amount ?? 0m,
            Category = dto.Category ?? string.Empty,
            Description = dto.Description,
            Currency = dto.Currency ?? "PHP",
            ExpenseDateUtc = dto.ExpenseDateUtc ?? DateTime.UtcNow,
            VendorOrSupplier = dto.VendorOrSupplier,
            InvoiceOrReceiptNumber = dto.InvoiceOrReceiptNumber
        });
    }

    public async Task DeleteRecordAsync(
        Guid id, 
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Expense ID cannot be empty.", nameof(id));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
    
}