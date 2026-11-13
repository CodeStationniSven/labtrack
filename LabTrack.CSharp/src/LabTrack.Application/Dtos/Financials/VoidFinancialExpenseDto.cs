using FluentValidation;

namespace LabTrack.Application.Dtos.Financials;

public sealed record VoidFinancialExpenseDto
{
    public Guid ExpenseId { get; init; }
    public string VoidReason { get; init; } = string.Empty;
}

public sealed class VoidFinancialExpenseDtoValidator : AbstractValidator<VoidFinancialExpenseDto>
{
    public VoidFinancialExpenseDtoValidator()
    {
        RuleFor(x => x.ExpenseId)
            .NotEmpty().WithMessage("Financial expense ID is required.");

        RuleFor(x => x.VoidReason)
            .NotEmpty().WithMessage("A reason for voiding the expense is required.")
            .MinimumLength(10).WithMessage("Void reason must be at least 10 characters long.")
            .MaximumLength(500).WithMessage("Void reason must not exceed 500 characters.");
    }
}