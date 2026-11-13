using FluentValidation;

namespace LabTrack.Application.Dtos.Replacement;

public sealed record CreateReplacementRequestDto
{
    public Guid LaboratoryId { get; init; }
    public Guid WorkstationId { get; init; }
    public Guid? ExistingAssetId { get; init; }

    public string ItemName { get; init; } = string.Empty;
    public string Category { get; init; } = "Peripheral";
    public int Quantity { get; init; } = 1;
    public string Urgency { get; init; } = "Medium";
    public string Justification { get; init; } = string.Empty;

    public decimal? EstimatedCost { get; init; }
    public string? PreferredVendor { get; init; }

    public Guid OriginalAssetId
    {
        get => ExistingAssetId ?? Guid.Empty;
        init => ExistingAssetId = value == Guid.Empty ? null : value;
    }

    public Guid? ReplacementAssetId { get; init; }

    public string Reason
    {
        get => Justification;
        init => Justification = value;
    }

    public Guid InitiatedByUserId { get; init; }
}

public sealed class CreateReplacementRequestDtoValidator : AbstractValidator<CreateReplacementRequestDto>
{
    public CreateReplacementRequestDtoValidator()
    {
        RuleFor(x => x.LaboratoryId)
            .NotEmpty().WithMessage("Laboratory ID is required.");

        RuleFor(x => x.WorkstationId)
            .NotEmpty().WithMessage("Workstation ID is required.");

        RuleFor(x => x.ItemName)
            .NotEmpty().WithMessage("Item name is required.")
            .MaximumLength(150).WithMessage("Item name must not exceed 150 characters.");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Item category is required.")
            .Must(c => c is "Peripheral" or "Component" or "Cable" or "Display" or "Workstation" or "Networking")
            .WithMessage("Category must be Peripheral, Component, Cable, Display, Workstation, or Networking.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than zero.")
            .LessThanOrEqualTo(100).WithMessage("Quantity must not exceed 100 units per request.");

        RuleFor(x => x.Urgency)
            .NotEmpty().WithMessage("Urgency level is required.")
            .Must(u => u is "Low" or "Medium" or "High" or "Critical")
            .WithMessage("Urgency must be Low, Medium, High, or Critical.");

        RuleFor(x => x.Justification)
            .NotEmpty().WithMessage("Justification for replacement is required.")
            .MinimumLength(10).WithMessage("Justification must be at least 10 characters long.")
            .MaximumLength(2000).WithMessage("Justification must not exceed 2000 characters.");

        RuleFor(x => x.EstimatedCost)
            .GreaterThanOrEqualTo(0).WithMessage("Estimated cost cannot be negative.")
            .When(x => x.EstimatedCost.HasValue);

        RuleFor(x => x.PreferredVendor)
            .MaximumLength(150).WithMessage("Preferred vendor name must not exceed 150 characters.")
            .When(x => !string.IsNullOrEmpty(x.PreferredVendor));
    }
}