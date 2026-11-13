namespace LabTrack.Application.Dtos.Analytics;

public sealed record FinancialSummaryDto
{
    // Maintenance & Repair Costs
    public decimal TotalMaintenanceExpense { get; init; }
    public decimal TotalHardwareReplacementExpense { get; init; }
    public decimal TotalPendingRepairEstimates { get; init; }

    // Operational & Consumable Expenses
    public decimal TotalConsumablesCost { get; init; }
    public decimal TotalOperationalExpense { get; init; }

    // Budget & Variance
    public decimal AllocatedBudget { get; init; }
    public decimal RemainingBudget => AllocatedBudget - TotalOverallExpense;
    public decimal BudgetUtilizationPercentage => AllocatedBudget > 0 
        ? Math.Round((TotalOverallExpense / AllocatedBudget) * 100, 2) 
        : 0m;

    // Totals & Period Metadata
    public decimal TotalOverallExpense => TotalMaintenanceExpense 
                                          + TotalHardwareReplacementExpense 
                                          + TotalConsumablesCost 
                                          + TotalOperationalExpense;

    public string Currency { get; init; } = "PHP";
    public DateTime PeriodStartUtc { get; init; }
    public DateTime PeriodEndUtc { get; init; }
    public DateTime GeneratedAtUtc { get; init; } = DateTime.UtcNow;

    // Breakdown Lists
    public IReadOnlyCollection<LaboratoryExpenseBreakdownDto> LaboratoryBreakdowns { get; init; } = Array.Empty<LaboratoryExpenseBreakdownDto>();
    public IReadOnlyCollection<ExpenseCategorySummaryDto> CategorySummaries { get; init; } = Array.Empty<ExpenseCategorySummaryDto>();
}

public sealed record LaboratoryExpenseBreakdownDto
{
    public Guid LaboratoryId { get; init; }
    public string LaboratoryName { get; init; } = string.Empty;
    public decimal TotalExpense { get; init; }
    public int TicketCount { get; init; }
    public int ReplacementCount { get; init; }
}

public sealed record ExpenseCategorySummaryDto
{
    public string CategoryName { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public double PercentageOfTotal { get; init; }
}