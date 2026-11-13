using FluentValidation;

namespace LabTrack.Application.Dtos.Tickets;

public sealed record UpdateRepairTicketStatusDto
{
    public string Status { get; init; } = string.Empty;
    public Guid? AssignedToUserId { get; init; }
    public string? ResolutionNotes { get; init; }
    public decimal? RepairCost { get; init; }
    public DateTime? ExpectedCompletionAtUtc { get; init; }

    public string? Notes
    {
        get => ResolutionNotes;
        init => ResolutionNotes = value;
    }
}

public sealed class UpdateRepairTicketStatusDtoValidator : AbstractValidator<UpdateRepairTicketStatusDto>
{
    public UpdateRepairTicketStatusDtoValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required.")
            .Must(BeValidStatus)
            .WithMessage("Status must be Open, Assigned, InProgress, Resolved, Closed, or Cancelled.");

        RuleFor(x => x.AssignedToUserId)
            .NotEmpty().WithMessage("Assigned user is required when status is Assigned or InProgress.")
            .When(x => x.Status is "Assigned" or "InProgress");

        RuleFor(x => x.ResolutionNotes)
            .NotEmpty().WithMessage("Resolution notes are required when status is Resolved.")
            .MaximumLength(2000).WithMessage("Resolution notes must not exceed 2000 characters.")
            .When(x => x.Status == "Resolved");

        RuleFor(x => x.RepairCost)
            .GreaterThanOrEqualTo(0).WithMessage("Repair cost cannot be negative.")
            .When(x => x.RepairCost.HasValue);
    }

    private static bool BeValidStatus(string status) =>
        status is "Open" or "Assigned" or "InProgress" or "Resolved" or "Closed" or "Cancelled";
}