using FluentValidation;

namespace LabTrack.Application.Dtos.Tickets;

public sealed record CreateRepairTicketDto
{
    public Guid LaboratoryId { get; init; }
    public Guid WorkstationId { get; init; }
    public Guid? AssetId { get; init; }

    public string Title { get; init; } = string.Empty;
    public string IssueDescription { get; init; } = string.Empty;
    public string Category { get; init; } = "Hardware";
    public string Priority { get; init; } = "Medium";

    public string? StepsToReproduce { get; init; }
    public string? AttachmentUrl { get; init; }

    public string ProblemDescription
    {
        get => IssueDescription;
        init => IssueDescription = value;
    }
}

public sealed class CreateRepairTicketDtoValidator : AbstractValidator<CreateRepairTicketDto>
{
    public CreateRepairTicketDtoValidator()
    {
        RuleFor(x => x.LaboratoryId)
            .NotEmpty().WithMessage("Laboratory ID is required.");

        RuleFor(x => x.WorkstationId)
            .NotEmpty().WithMessage("Workstation ID is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Ticket title is required.")
            .MaximumLength(150).WithMessage("Ticket title must not exceed 150 characters.");

        RuleFor(x => x.IssueDescription)
            .NotEmpty().WithMessage("Issue description is required.")
            .MinimumLength(10).WithMessage("Issue description must be at least 10 characters long.")
            .MaximumLength(2000).WithMessage("Issue description must not exceed 2000 characters.");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Ticket category is required.")
            .Must(c => c is "Hardware" or "Software" or "Network" or "Peripheral" or "Power")
            .WithMessage("Category must be Hardware, Software, Network, Peripheral, or Power.");

        RuleFor(x => x.Priority)
            .NotEmpty().WithMessage("Priority level is required.")
            .Must(p => p is "Low" or "Medium" or "High" or "Critical")
            .WithMessage("Priority must be Low, Medium, High, or Critical.");

        RuleFor(x => x.StepsToReproduce)
            .MaximumLength(1000).WithMessage("Steps to reproduce must not exceed 1000 characters.")
            .When(x => !string.IsNullOrEmpty(x.StepsToReproduce));

        RuleFor(x => x.AttachmentUrl)
            .MaximumLength(500).WithMessage("Attachment URL must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.AttachmentUrl));
    }
}