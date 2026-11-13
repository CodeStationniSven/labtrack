using FluentValidation;

namespace LabTrack.Application.Dtos.Assets;

public sealed record UpdateAssetAiMetricsDto
{
    public Guid AssetId { get; init; }
    
    // AI Predictive Maintenance & Health Metrics
    public double HealthScore { get; init; } // e.g., 0.0 to 100.0
    public double PredictedFailureProbability { get; init; } // e.g., 0.0 to 1.0 (0% to 100%)
    public int EstimatedRemainingUsefulLifeDays { get; init; }
    public string ReliabilityStatus { get; init; } = "Optimal"; // e.g., Optimal, Degraded, Critical Risk
    
    // AI Diagnostics & Recommendations
    public string? AnomaliesDetected { get; init; }
    public string? RecommendedAction { get; init; }
    public DateTime? SuggestedNextMaintenanceDateUtc { get; init; }
    public DateTime AnalyzedAtUtc { get; init; } = DateTime.UtcNow;
}

public sealed class UpdateAssetAiMetricsDtoValidator : AbstractValidator<UpdateAssetAiMetricsDto>
{
    public UpdateAssetAiMetricsDtoValidator()
    {
        RuleFor(x => x.AssetId)
            .NotEmpty().WithMessage("Asset ID is required.");

        RuleFor(x => x.HealthScore)
            .InclusiveBetween(0.0, 100.0)
            .WithMessage("Health score must be between 0.0 and 100.0.");

        RuleFor(x => x.PredictedFailureProbability)
            .InclusiveBetween(0.0, 1.0)
            .WithMessage("Predicted failure probability must be between 0.0 and 1.0.");

        RuleFor(x => x.EstimatedRemainingUsefulLifeDays)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Estimated remaining useful life days cannot be negative.");

        RuleFor(x => x.ReliabilityStatus)
            .NotEmpty().WithMessage("Reliability status is required.")
            .MaximumLength(50).WithMessage("Reliability status must not exceed 50 characters.");

        RuleFor(x => x.AnomaliesDetected)
            .MaximumLength(1000).WithMessage("Anomalies description must not exceed 1000 characters.")
            .When(x => !string.IsNullOrEmpty(x.AnomaliesDetected));

        RuleFor(x => x.RecommendedAction)
            .MaximumLength(1000).WithMessage("Recommended action must not exceed 1000 characters.")
            .When(x => !string.IsNullOrEmpty(x.RecommendedAction));
    }
}