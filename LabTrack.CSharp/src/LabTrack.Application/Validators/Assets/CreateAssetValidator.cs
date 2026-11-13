using FluentValidation;
using LabTrack.Application.Commands.Assets;

namespace LabTrack.Application.Validators.Assets;

public class CreateAssetValidator : AbstractValidator<CreateAssetCommand>
{
    public CreateAssetValidator()
    {
        RuleFor(x => x.AssetTag)
            .NotEmpty()
            .WithMessage("Asset tag is required.")
            .MaximumLength(50)
            .WithMessage("Asset tag must not exceed 50 characters.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Asset name is required.")
            .MaximumLength(150)
            .WithMessage("Asset name must not exceed 150 characters.");

        RuleFor(x => x.Category)
            .NotEmpty()
            .WithMessage("Category is required.")
            .MaximumLength(100)
            .WithMessage("Category must not exceed 100 characters.");

        RuleFor(x => x.SerialNumber)
            .MaximumLength(100)
            .WithMessage("Serial number must not exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.SerialNumber));

        RuleFor(x => x.Status)
            .NotEmpty()
            .WithMessage("Status is required.")
            .MaximumLength(50)
            .WithMessage("Status must not exceed 50 characters.");

        RuleFor(x => x.LaboratoryId)
            .NotEmpty()
            .WithMessage("Laboratory ID is required.");
    }
}