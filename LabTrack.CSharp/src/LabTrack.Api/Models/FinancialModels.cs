using System.ComponentModel.DataAnnotations;

namespace LabTrack.Api.Models;

public class CreateExpenseRequest
{
    [Required]
    public string Category { get; set; } = string.Empty;

    [Required]
    public decimal Amount { get; set; }

    public string Description { get; set; } = string.Empty;
}

public class ExpenseResponse
{
    public string Id { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime TransactionDateUtc { get; set; }
}
