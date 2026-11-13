using FluentValidation;

namespace LabTrack.Application.Dtos.Usage;

public sealed record EndSessionDto
{
    public Guid SessionId { get; init; }
    public Guid WorkstationId { get; init; }
    public Guid StudentId { get; init; }

    public DateTime EndedAtUtc { get; init; } = DateTime.UtcNow;
    public string? WorkstationCondition { get; init; } = "Good"; // e.g., Good, MinorIssue, Damaged, Cleaned
    public string? Remarks { get; init; }
    public bool ReportedIssueDuringSession { get; init; }
}

public sealed class EndSessionDtoValidator : AbstractValidator<EndSessionDto>
{
    public EndSessionDtoValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty().WithMessage("Session ID is required.");

        RuleFor(x => x.WorkstationId)
            .NotEmpty().WithMessage("Workstation ID is required.");

        RuleFor(x => x.StudentId)
            .NotEmpty().WithMessage("Student ID is required.");

        RuleFor(x => x.EndedAtUtc)
            .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5))
            .WithMessage("Session end timestamp cannot be in the future.");

        RuleFor(x => x.WorkstationCondition)
            .Must(c => c is null or "Good" or "MinorIssue" or "Damaged" or "Cleaned")
            .WithMessage("Workstation condition must be Good, MinorIssue, Damaged, or Cleaned.");

        RuleFor(x => x.Remarks)
            .MaximumLength(500).WithMessage("Remarks must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.Remarks));
    }
}