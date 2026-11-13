using FluentValidation;

namespace LabTrack.Application.Dtos.Reports;

public sealed record UpdateProblemReportStatusDto
{
    public string Status { get; init; } = string.Empty;
    public Guid? AssignedToUserId { get; init; }
    public string? ResolutionSummary { get; init; }
    public string? DismissalReason { get; init; }

    public string? ResolutionNotes
    {
        get => ResolutionSummary;
        init => ResolutionSummary = value;
    }
}

public sealed class UpdateProblemReportStatusDtoValidator : AbstractValidator<UpdateProblemReportStatusDto>
{
    public UpdateProblemReportStatusDtoValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(s => s is "Submitted" or "UnderReview" or "InProgress" or "Resolved" or "Dismissed")
            .WithMessage("Status must be Submitted, UnderReview, InProgress, Resolved, or Dismissed.");

        RuleFor(x => x.ResolutionNotes)
            .MaximumLength(2000)
            .When(x => !string.IsNullOrEmpty(x.ResolutionNotes));
    }
}