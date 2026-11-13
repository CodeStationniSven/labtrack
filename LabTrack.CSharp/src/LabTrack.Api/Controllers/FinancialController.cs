using LabTrack.Application.Interfaces;
using LabTrack.Api.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LabTrack.Api.Controllers;

public sealed record FinancialTransactionDto(
    Guid Id,
    Guid? AssetId,
    Guid? RepairTicketId,
    string TransactionType,
    decimal Amount,
    string Currency,
    string Description,
    DateTime TransactionDate,
    DateTime CreatedAt
);

public sealed record CreateFinancialTransactionDto(
    Guid? AssetId,
    Guid? RepairTicketId,
    string TransactionType,
    decimal Amount,
    string Currency,
    string Description,
    DateTime TransactionDate
);

public sealed record FinancialSummaryDto(
    decimal TotalExpenditure,
    decimal TotalRepairCosts,
    decimal TotalAssetAcquisitions,
    int TotalTransactionsCount
);

[ApiController]
[Route("api/v1/financials")]
[Authorize(Roles = "TECHNICIAN,ADMIN,ADMIN_TECHNICIAN")]
public sealed class FinancialController : ControllerBase
{
    private readonly IFinancialService _financialService;
    private readonly ILogger<FinancialController> _logger;

    public FinancialController(
        IFinancialService financialService,
        ILogger<FinancialController> logger)
    {
        _financialService = financialService;
        _logger = logger;
    }

    [HttpGet("transactions")]
    public async Task<ActionResult<ApiResponse<PaginationResponse<FinancialTransactionDto>>>> GetTransactions(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? type = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1 || pageSize < 1)
        {
            return BadRequest(ApiResponse<PaginationResponse<FinancialTransactionDto>>.Fail(
                "INVALID_PAGINATION",
                "Page number and page size must be greater than zero."));
        }

        if (startDate.HasValue && endDate.HasValue && startDate > endDate)
        {
            return BadRequest(ApiResponse<PaginationResponse<FinancialTransactionDto>>.Fail(
                "INVALID_DATE_RANGE",
                "Start date cannot be later than end date."));
        }

        try
        {
            var result = await _financialService.GetTransactionsAsync(pageNumber, pageSize, type, startDate, endDate, cancellationToken);
            return Ok(ApiResponse<PaginationResponse<FinancialTransactionDto>>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching financial transactions");
            return StatusCode(500, ApiResponse<PaginationResponse<FinancialTransactionDto>>.Fail(
                "FINANCIAL_ERROR",
                "An error occurred while fetching financial transactions."));
        }
    }

    [HttpGet("transactions/{id:guid}")]
    public async Task<ActionResult<ApiResponse<FinancialTransactionDto>>> GetTransactionById(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var transaction = await _financialService.GetTransactionByIdAsync(id, cancellationToken);
            if (transaction is null)
            {
                return NotFound(ApiResponse<FinancialTransactionDto>.Fail(
                    "TRANSACTION_NOT_FOUND",
                    $"Financial transaction with ID '{id}' was not found."));
            }

            return Ok(ApiResponse<FinancialTransactionDto>.Ok(transaction));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching financial transaction with ID {TransactionId}", id);
            return StatusCode(500, ApiResponse<FinancialTransactionDto>.Fail(
                "FINANCIAL_ERROR",
                "An error occurred while fetching the transaction record."));
        }
    }

    [HttpPost("transactions")]
    public async Task<ActionResult<ApiResponse<FinancialTransactionDto>>> CreateTransaction(
        [FromBody] CreateFinancialTransactionDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.TransactionType) || request.Amount <= 0)
        {
            return BadRequest(ApiResponse<FinancialTransactionDto>.Fail(
                "INVALID_TRANSACTION_DATA",
                "Transaction type and a positive amount are required."));
        }

        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var recordedByUserId))
        {
            return Unauthorized(ApiResponse<FinancialTransactionDto>.Fail(
                "UNAUTHORIZED",
                "User identity is invalid or missing."));
        }

        try
        {
            var createdTransaction = await _financialService.CreateTransactionAsync(request, recordedByUserId, cancellationToken);
            return CreatedAtAction(
                nameof(GetTransactionById),
                new { id = createdTransaction.Id },
                ApiResponse<FinancialTransactionDto>.Ok(createdTransaction));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating financial transaction");
            return StatusCode(500, ApiResponse<FinancialTransactionDto>.Fail("FINANCIAL_CREATE_ERROR", $"{ex.Message} | Inner: {ex.InnerException?.Message}"));
        }
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<FinancialSummaryDto>>> GetFinancialSummary(
        CancellationToken cancellationToken)
    {
        try
        {
            var summary = await _financialService.GetFinancialSummaryAsync(cancellationToken);
            return Ok(ApiResponse<FinancialSummaryDto>.Ok(summary));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching financial summary");
            return StatusCode(500, ApiResponse<FinancialSummaryDto>.Fail(
                "FINANCIAL_ERROR",
                "An error occurred while calculating financial summary metrics."));
        }
    }
}



