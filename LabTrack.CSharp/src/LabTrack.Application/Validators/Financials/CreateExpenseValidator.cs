using FluentValidation;
using LabTrack.Application.Commands.AiChat.Financials;

namespace LabTrack.Application.Validators.Financials;

public class CreateExpenseValidator : AbstractValidator<CreateExpenseCommand>
{
    public CreateExpenseValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Expense amount must be greater than zero.");

        RuleFor(x => x.Category)
            .NotEmpty()
            .WithMessage("Expense category is required.")
            .MaximumLength(100)
            .WithMessage("Expense category must not exceed 100 characters.");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Description is required.")
            .MaximumLength(500)
            .WithMessage("Description must not exceed 500 characters.");

        RuleFor(x => x.LaboratoryId)
            .NotEmpty()
            .WithMessage("Laboratory ID is required.");

        RuleFor(x => x.RecordedByUserId)
            .NotEmpty()
            .WithMessage("Recorded By User ID is required.");
    }
}
