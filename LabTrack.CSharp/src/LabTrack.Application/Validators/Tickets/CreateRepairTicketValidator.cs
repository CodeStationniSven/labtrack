using FluentValidation;
using LabTrack.Application.Commands.AiChat.Tickets;

namespace LabTrack.Application.Validators.Tickets;

public class CreateRepairTicketValidator : AbstractValidator<CreateRepairTicketCommand>
{
    public CreateRepairTicketValidator()
    {
        RuleFor(x => x.AssetId)
            .NotEmpty()
            .WithMessage("Asset ID is required.");

        RuleFor(x => x.ProblemDescription)
            .NotEmpty()
            .WithMessage("Problem description is required.")
            .MaximumLength(1000)
            .WithMessage("Problem description must not exceed 1000 characters.");

        RuleFor(x => x.Priority)
            .NotEmpty()
            .WithMessage("Priority level is required.")
            .MaximumLength(50)
            .WithMessage("Priority level must not exceed 50 characters.");

        RuleFor(x => x.ReportedByUserId)
            .NotEmpty()
            .WithMessage("Reported By User ID is required.");
    }
}
