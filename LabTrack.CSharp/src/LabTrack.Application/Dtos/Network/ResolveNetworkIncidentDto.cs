using FluentValidation;

namespace LabTrack.Application.Dtos.Network;

public sealed record ResolveNetworkIncidentDto
{
    public Guid IncidentId { get; init; }
    public string ResolutionNotes { get; init; } = string.Empty;
    public string ActionsTaken { get; init; } = string.Empty;
    public DateTime ResolvedAtUtc { get; init; } = DateTime.UtcNow;
}

public sealed class ResolveNetworkIncidentDtoValidator : AbstractValidator<ResolveNetworkIncidentDto>
{
    public ResolveNetworkIncidentDtoValidator()
    {
        RuleFor(x => x.IncidentId)
            .NotEmpty().WithMessage("Network incident ID is required.");

        RuleFor(x => x.ResolutionNotes)
            .NotEmpty().WithMessage("Resolution notes are required.")
            .MinimumLength(10).WithMessage("Resolution notes must be at least 10 characters long.")
            .MaximumLength(2000).WithMessage("Resolution notes must not exceed 2000 characters.");

        RuleFor(x => x.ActionsTaken)
            .NotEmpty().WithMessage("Actions taken must be specified.")
            .MaximumLength(1000).WithMessage("Actions taken description must not exceed 1000 characters.");

        RuleFor(x => x.ResolvedAtUtc)
            .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5))
            .WithMessage("Resolution timestamp cannot be set in the future.");
    }
}