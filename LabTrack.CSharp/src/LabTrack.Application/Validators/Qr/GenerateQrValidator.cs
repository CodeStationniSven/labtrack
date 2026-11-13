using FluentValidation;
using LabTrack.Application.Commands.AiChat.Qr;

namespace LabTrack.Application.Validators.Qr;

public class GenerateQrValidator : AbstractValidator<GenerateQrCommand>
{
    public GenerateQrValidator()
    {
        RuleFor(x => x.EntityType)
            .NotEmpty()
            .WithMessage("Entity type is required.")
            .MaximumLength(100)
            .WithMessage("Entity type must not exceed 100 characters.");

        RuleFor(x => x.EntityId)
            .NotEmpty()
            .WithMessage("Entity ID is required.");
    }
}
