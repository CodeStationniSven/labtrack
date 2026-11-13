using FluentValidation;
using LabTrack.Application.Commands.Replacement;

namespace LabTrack.Application.Validators.Replacement;

public class CreateReplacementValidator : AbstractValidator<CreateReplacementCommand>
{
    public CreateReplacementValidator()
    {
        RuleFor(x => x.OriginalAssetId)
            .NotEmpty()
            .WithMessage("Original asset ID is required.");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Replacement reason is required.")
            .MaximumLength(500)
            .WithMessage("Replacement reason must not exceed 500 characters.");

        RuleFor(x => x.InitiatedByUserId)
            .NotEmpty()
            .WithMessage("Initiator user ID is required.");

        RuleFor(x => x.ReplacementAssetId)
            .NotEqual(x => x.OriginalAssetId)
            .WithMessage("Replacement asset cannot be the same as the original asset.")
            .When(x => x.ReplacementAssetId.HasValue);
    }
}