namespace LabTrack.Application.Dtos.Analytics;

public sealed record FinancialAnalyticsDto
{
    public Guid? LaboratoryId { get; init; }
    public string? LaboratoryName { get; init; }
    public DateTime? StartDateUtc { get; init; }
    public DateTime? EndDateUtc { get; init; }
    public decimal TotalExpenditure { get; init; }
    public decimal TotalRepairCost { get; init; }
    public decimal TotalMaintenanceCost { get; init; }
    public decimal TotalReplacementCost { get; init; }
    public decimal TotalConsumablesCost { get; init; }
    public decimal TotalOperationalCost { get; init; }
    public int TransactionCount { get; init; }
    public string Currency { get; init; } = "PHP";
    public IReadOnlyCollection<MonthlyCostDto> MonthlyBreakdown { get; init; } = Array.Empty<MonthlyCostDto>();
    public DateTime GeneratedAtUtc { get; init; } = DateTime.UtcNow;
}

public sealed record MonthlyCostDto
{
    public int Year { get; init; }
    public int Month { get; init; }
    public decimal Amount { get; init; }
    public int TransactionCount { get; init; }
}