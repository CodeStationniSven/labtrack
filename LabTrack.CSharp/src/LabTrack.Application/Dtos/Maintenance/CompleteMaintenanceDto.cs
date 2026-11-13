using FluentValidation;

namespace LabTrack.Application.Dtos.Maintenance;

public sealed record CompleteMaintenanceDto
{
    public Guid TicketId { get; init; }
    public string ResolutionNotes { get; init; } = string.Empty;
    public decimal? TotalCost { get; init; }
    public string? ReplacedParts { get; init; }
    public DateTime CompletedAtUtc { get; init; } = DateTime.UtcNow;

    
    public string CompletionNotes
    {
        get => ResolutionNotes;
        init => ResolutionNotes = value;
    }
    
}

public sealed class CompleteMaintenanceDtoValidator : AbstractValidator<CompleteMaintenanceDto>
{
    public CompleteMaintenanceDtoValidator()
    {
        RuleFor(x => x.TicketId)
            .NotEmpty().WithMessage("Maintenance ticket ID is required.");

        RuleFor(x => x.ResolutionNotes)
            .NotEmpty().WithMessage("Resolution notes are required.")
            .MinimumLength(10).WithMessage("Resolution notes must be at least 10 characters long.")
            .MaximumLength(2000).WithMessage("Resolution notes must not exceed 2000 characters.");

        RuleFor(x => x.TotalCost)
            .GreaterThanOrEqualTo(0).WithMessage("Total cost cannot be negative.")
            .When(x => x.TotalCost.HasValue);

        RuleFor(x => x.ReplacedParts)
            .MaximumLength(1000).WithMessage("Replaced parts summary must not exceed 1000 characters.")
            .When(x => !string.IsNullOrEmpty(x.ReplacedParts));

        RuleFor(x => x.CompletedAtUtc)
            .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5))
            .WithMessage("Completion date cannot be set in the future.");
    }
}