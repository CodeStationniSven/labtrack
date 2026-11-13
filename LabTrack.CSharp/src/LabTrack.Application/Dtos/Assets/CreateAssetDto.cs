using FluentValidation;

namespace LabTrack.Application.Dtos.Assets;

public sealed record CreateAssetDto
{
    public string AssetTag { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string? SerialNumber { get; init; }
    public string? Brand { get; init; }
    public string? Model { get; init; }

    public string Status { get; init; } = "Available";   // ← IDINAGDAG LANG

    // Optional initial assignments
    public Guid? LaboratoryId { get; init; }
    public Guid? WorkstationId { get; init; }

    // Lifecycle details
    public DateTime? PurchaseDate { get; init; }
    public decimal? PurchaseCost { get; init; }
    public DateTime? WarrantyExpirationDate { get; init; }
}

public sealed class CreateAssetDtoValidator : AbstractValidator<CreateAssetDto>
{
    public CreateAssetDtoValidator()
    {
        RuleFor(x => x.AssetTag)
            .NotEmpty().WithMessage("Asset tag is required.")
            .MaximumLength(50).WithMessage("Asset tag must not exceed 50 characters.")
            .Matches(@"^[a-zA-Z0-9\-_]+$").WithMessage("Asset tag can only contain letters, numbers, hyphens, and underscores.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Asset name is required.")
            .MaximumLength(100).WithMessage("Asset name must not exceed 100 characters.");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Category is required.")
            .MaximumLength(50).WithMessage("Category must not exceed 50 characters.");

        RuleFor(x => x.SerialNumber)
            .MaximumLength(100).WithMessage("Serial number must not exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.SerialNumber));

        RuleFor(x => x.Brand)
            .MaximumLength(50).WithMessage("Brand must not exceed 50 characters.")
            .When(x => !string.IsNullOrEmpty(x.Brand));

        RuleFor(x => x.Model)
            .MaximumLength(50).WithMessage("Model must not exceed 50 characters.")
            .When(x => !string.IsNullOrEmpty(x.Model));

        RuleFor(x => x.PurchaseCost)
            .GreaterThanOrEqualTo(0).WithMessage("Purchase cost cannot be negative.")
            .When(x => x.PurchaseCost.HasValue);

        RuleFor(x => x.WarrantyExpirationDate)
            .GreaterThanOrEqualTo(x => x.PurchaseDate!.Value)
            .WithMessage("Warranty expiration date cannot be earlier than the purchase date.")
            .When(x => x.PurchaseDate.HasValue && x.WarrantyExpirationDate.HasValue);

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required.")
            .Must(BeValidStatus)
            .WithMessage("Status must be Available, InUse, UnderMaintenance, or Retired.");
    }

    private static bool BeValidStatus(string status) =>
        status is "Available" or "InUse" or "UnderMaintenance" or "Retired";
}