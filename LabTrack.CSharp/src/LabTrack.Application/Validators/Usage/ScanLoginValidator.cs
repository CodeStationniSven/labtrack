using FluentValidation;
using LabTrack.Application.Commands.AiChat.Usage;

namespace LabTrack.Application.Validators.Usage;

public class ScanLoginValidator : AbstractValidator<ScanLoginCommand>
{
    public ScanLoginValidator()
    {
        RuleFor(x => x.QrData)
            .NotEmpty()
            .WithMessage("QR data is required.")
            .MaximumLength(500)
            .WithMessage("QR data must not exceed 500 characters.");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required.");

        RuleFor(x => x.WorkstationId)
            .NotEmpty()
            .WithMessage("Workstation ID is required.");
    }
}
