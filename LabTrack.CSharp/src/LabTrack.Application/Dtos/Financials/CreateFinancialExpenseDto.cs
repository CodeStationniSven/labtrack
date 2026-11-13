using FluentValidation;

namespace LabTrack.Application.Dtos.Financials;

public sealed record CreateFinancialExpenseDto
{
    public Guid LaboratoryId { get; init; }
    public Guid? WorkstationId { get; init; }
    public Guid? AssetId { get; init; }

    public string Title { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty; // e.g., Maintenance, HardwareUpgrade, Utility, SoftwareLicense, Consumables
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "PHP";

    public DateTime ExpenseDateUtc { get; init; } = DateTime.UtcNow;
    public string? Description { get; init; }
    public string? VendorOrSupplier { get; init; }
    public string? InvoiceOrReceiptNumber { get; init; }

    public Guid CreatedByUserId { get; init; }   // ← IDINAGDAG
}

public sealed class CreateFinancialExpenseDtoValidator : AbstractValidator<CreateFinancialExpenseDto>
{
    public CreateFinancialExpenseDtoValidator()
    {
        RuleFor(x => x.LaboratoryId)
            .NotEmpty().WithMessage("Laboratory ID is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Expense title is required.")
            .MaximumLength(150).WithMessage("Title must not exceed 150 characters.");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Expense category is required.")
            .MaximumLength(50).WithMessage("Category must not exceed 50 characters.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Expense amount must be greater than zero.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency code is required.")
            .Length(3).WithMessage("Currency code must be a 3-character ISO code (e.g., PHP, USD).");

        RuleFor(x => x.ExpenseDateUtc)
            .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5))
            .WithMessage("Expense date cannot be set far into the future.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.VendorOrSupplier)
            .MaximumLength(100).WithMessage("Vendor or supplier name must not exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.VendorOrSupplier));

        RuleFor(x => x.InvoiceOrReceiptNumber)
            .MaximumLength(100).WithMessage("Invoice or receipt number must not exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.InvoiceOrReceiptNumber));

        RuleFor(x => x.CreatedByUserId)
            .NotEmpty().WithMessage("Creator user ID is required.");
    }
}