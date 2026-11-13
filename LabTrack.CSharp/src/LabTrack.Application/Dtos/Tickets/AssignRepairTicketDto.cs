using FluentValidation;

namespace LabTrack.Application.Dtos.Tickets;

public sealed record AssignRepairTicketDto
{
    public Guid TicketId { get; init; }
    public Guid TechnicianId { get; init; }
    public string Priority { get; init; } = "Medium"; // e.g., Low, Medium, High, Critical
    public DateTime? EstimatedCompletionAtUtc { get; init; }
    public string? AssignmentNotes { get; init; }
}

public sealed class AssignRepairTicketDtoValidator : AbstractValidator<AssignRepairTicketDto>
{
    public AssignRepairTicketDtoValidator()
    {
        RuleFor(x => x.TicketId)
            .NotEmpty().WithMessage("Ticket ID is required.");

        RuleFor(x => x.TechnicianId)
            .NotEmpty().WithMessage("Technician ID is required.");

        RuleFor(x => x.Priority)
            .NotEmpty().WithMessage("Priority level is required.")
            .Must(p => p is "Low" or "Medium" or "High" or "Critical")
            .WithMessage("Priority must be Low, Medium, High, or Critical.");

        RuleFor(x => x.EstimatedCompletionAtUtc)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("Estimated completion date must be in the future.")
            .When(x => x.EstimatedCompletionAtUtc.HasValue);

        RuleFor(x => x.AssignmentNotes)
            .MaximumLength(1000).WithMessage("Assignment notes must not exceed 1000 characters.")
            .When(x => !string.IsNullOrEmpty(x.AssignmentNotes));
    }
}