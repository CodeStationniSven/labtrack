using FluentValidation;

namespace LabTrack.Application.Dtos.Maintenance;

public sealed record UpdateMaintenanceScheduleDto
{
    public Guid Id { get; init; }
    public Guid LaboratoryId { get; init; }
    public Guid? WorkstationId { get; init; }
    public Guid? AssetId { get; init; }

    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string MaintenanceType { get; init; } = "Preventive";
    public string RecurrencePattern { get; init; } = "OneTime";
    public bool IsActive { get; init; } = true;

    public DateTime ScheduledStartDateUtc { get; init; }
    public DateTime? ScheduledEndDateUtc { get; init; }
    public Guid? AssignedTechnicianId { get; init; }

    
    public DateTime ScheduledDateUtc
    {
        get => ScheduledStartDateUtc;
        init => ScheduledStartDateUtc = value;
    }

    public string Status { get; init; } = "Scheduled";
    public int? RecurringIntervalDays { get; init; }
    
}

public sealed class UpdateMaintenanceScheduleDtoValidator : AbstractValidator<UpdateMaintenanceScheduleDto>
{
    public UpdateMaintenanceScheduleDtoValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Maintenance schedule ID is required.");

        RuleFor(x => x.LaboratoryId)
            .NotEmpty().WithMessage("Laboratory ID is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Schedule title is required.")
            .MaximumLength(150).WithMessage("Title must not exceed 150 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Schedule description is required.")
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.");

        RuleFor(x => x.MaintenanceType)
            .NotEmpty().WithMessage("Maintenance type is required.")
            .Must(t => t is "Preventive" or "Corrective" or "SoftwareUpdate" or "Inspection")
            .WithMessage("Maintenance type must be Preventive, Corrective, SoftwareUpdate, or Inspection.");

        RuleFor(x => x.RecurrencePattern)
            .NotEmpty().WithMessage("Recurrence pattern is required.")
            .Must(r => r is "OneTime" or "Daily" or "Weekly" or "Monthly" or "Quarterly" or "BiAnnually" or "Annually")
            .WithMessage("Invalid recurrence pattern specified.");

        RuleFor(x => x.ScheduledEndDateUtc)
            .GreaterThan(x => x.ScheduledStartDateUtc)
            .WithMessage("Scheduled end date must be after the start date.")
            .When(x => x.ScheduledEndDateUtc.HasValue);
    }
}