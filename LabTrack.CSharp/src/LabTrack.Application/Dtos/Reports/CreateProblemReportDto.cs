using FluentValidation;

namespace LabTrack.Application.Dtos.Reports;

public sealed record CreateProblemReportDto
{
    public Guid ReportedByUserId { get; init; }

    public Guid ReporterId
    {
        get => ReportedByUserId;
        init => ReportedByUserId = value;
    }

    public Guid LaboratoryId { get; init; }
    public Guid? WorkstationId { get; init; }
    public Guid? AssetId { get; init; }

    public string Title { get; init; } = string.Empty;
    public string Category { get; init; } = "Hardware";
    public string Severity { get; init; } = "Medium";
    public string Description { get; init; } = string.Empty;

    public string? StepsToReproduce { get; init; }
    public string? AttachmentUrl { get; init; }
    public DateTime ReportedAtUtc { get; init; } = DateTime.UtcNow;
}

public sealed class CreateProblemReportDtoValidator : AbstractValidator<CreateProblemReportDto>
{
    public CreateProblemReportDtoValidator()
    {
        RuleFor(x => x.LaboratoryId)
            .NotEmpty().WithMessage("Laboratory ID is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Report title is required.")
            .MaximumLength(150).WithMessage("Title must not exceed 150 characters.");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Problem category is required.")
            .Must(c => c is "Hardware" or "Software" or "Network" or "Facility" or "Safety")
            .WithMessage("Category must be Hardware, Software, Network, Facility, or Safety.");

        RuleFor(x => x.Severity)
            .NotEmpty().WithMessage("Severity level is required.")
            .Must(s => s is "Low" or "Medium" or "High" or "Critical")
            .WithMessage("Severity must be Low, Medium, High, or Critical.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Problem description is required.")
            .MinimumLength(10).WithMessage("Description must be at least 10 characters long.")
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.");

        RuleFor(x => x.StepsToReproduce)
            .MaximumLength(1000).WithMessage("Steps to reproduce must not exceed 1000 characters.")
            .When(x => !string.IsNullOrEmpty(x.StepsToReproduce));

        RuleFor(x => x.AttachmentUrl)
            .MaximumLength(500).WithMessage("Attachment URL must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.AttachmentUrl));

        RuleFor(x => x.ReportedAtUtc)
            .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5))
            .WithMessage("Report timestamp cannot be set in the future.");
    }
}