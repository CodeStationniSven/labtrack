using FluentValidation;

namespace LabTrack.Application.Dtos.Replacement;

public sealed record UpdateReplacementStatusDto
{
    public string Status { get; init; } = string.Empty;
    public Guid? ReviewedByUserId { get; init; }
    public string? DecisionNotes { get; init; }
    public decimal? ApprovedCost { get; init; }
    public DateTime? EstimatedFulfillmentDateUtc { get; init; }

    public string? Notes
    {
        get => DecisionNotes;
        init => DecisionNotes = value;
    }
}

public sealed class UpdateReplacementStatusDtoValidator : AbstractValidator<UpdateReplacementStatusDto>
{
    public UpdateReplacementStatusDtoValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required.")
            .Must(BeValidStatus)
            .WithMessage("Status must be Pending, Approved, Rejected, Fulfilled, or Cancelled.");

        RuleFor(x => x.ReviewedByUserId)
            .NotEmpty().WithMessage("Reviewer user ID is required when status is Approved, Rejected, or Fulfilled.")
            .When(x => x.Status is "Approved" or "Rejected" or "Fulfilled");

        RuleFor(x => x.DecisionNotes)
            .NotEmpty().WithMessage("Decision notes are required when status is Rejected.")
            .MaximumLength(1000).WithMessage("Decision notes must not exceed 1000 characters.")
            .When(x => x.Status == "Rejected");

        RuleFor(x => x.ApprovedCost)
            .GreaterThanOrEqualTo(0).WithMessage("Approved cost cannot be negative.")
            .When(x => x.ApprovedCost.HasValue);

        RuleFor(x => x.EstimatedFulfillmentDateUtc)
            .GreaterThan(DateTime.UtcNow).WithMessage("Estimated fulfillment date must be in the future.")
            .When(x => x.EstimatedFulfillmentDateUtc.HasValue);
    }

    private static bool BeValidStatus(string status) =>
        status is "Pending" or "Approved" or "Rejected" or "Fulfilled" or "Cancelled";
}