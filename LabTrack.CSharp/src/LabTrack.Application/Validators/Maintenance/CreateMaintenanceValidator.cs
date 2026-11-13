using FluentValidation;
using LabTrack.Application.Commands.AiChat.Maintenance;

namespace LabTrack.Application.Validators.Maintenance;

public class CreateMaintenanceValidator : AbstractValidator<CreateMaintenanceCommand>
{
    public CreateMaintenanceValidator()
    {
        RuleFor(x => x.AssetId)
            .NotEmpty()
            .WithMessage("Asset ID is required.");

        RuleFor(x => x.MaintenanceType)
            .NotEmpty()
            .WithMessage("Maintenance type is required.")
            .MaximumLength(100)
            .WithMessage("Maintenance type must not exceed 100 characters.");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Description is required.")
            .MaximumLength(500)
            .WithMessage("Description must not exceed 500 characters.");

        RuleFor(x => x.ScheduledDateUtc)
            .GreaterThanOrEqualTo(DateTime.UtcNow.Date)
            .WithMessage("Scheduled date cannot be in the past.");

        RuleFor(x => x.RecurringIntervalDays)
            .GreaterThan(0)
            .WithMessage("Recurring interval days must be greater than zero.")
            .When(x => x.RecurringIntervalDays.HasValue);
    }
}
