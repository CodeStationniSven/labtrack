using FluentValidation;
using LabTrack.Application.Commands.AiChat.Reports;

namespace LabTrack.Application.Validators.Reports;

public class CreateProblemReportValidator : AbstractValidator<CreateProblemReportCommand>
{
    public CreateProblemReportValidator()
    {
        RuleFor(x => x.ReporterId)
            .NotEmpty()
            .WithMessage("Reporter ID is required.");

        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title is required.")
            .MaximumLength(150)
            .WithMessage("Title must not exceed 150 characters.");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Description is required.")
            .MaximumLength(1000)
            .WithMessage("Description must not exceed 1000 characters.");

        RuleFor(x => x.Severity)
            .NotEmpty()
            .WithMessage("Severity level is required.")
            .MaximumLength(50)
            .WithMessage("Severity level must not exceed 50 characters.");

        RuleFor(x => x.LaboratoryId)
            .NotEmpty()
            .WithMessage("Laboratory ID is required.")
            .When(x => !x.AssetId.HasValue && !x.WorkstationId.HasValue);
    }
}
