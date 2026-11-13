namespace LabTrack.Application.Dtos.Maintenance;

public sealed record MaintenanceScheduleDto
{
    public Guid Id { get; init; }
    public Guid LaboratoryId { get; init; }
    public string LaboratoryName { get; init; } = string.Empty;
    public Guid? WorkstationId { get; init; }
    public string? WorkstationName { get; init; }
    public Guid? AssetId { get; init; }
    public string? AssetName { get; init; }

    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string MaintenanceType { get; init; } = "Preventive";
    public string RecurrencePattern { get; init; } = "OneTime";
    public bool IsActive { get; init; } = true;

    // Schedule Window
    public DateTime ScheduledStartDateUtc { get; init; }
    public DateTime? ScheduledEndDateUtc { get; init; }
    public DateTime? LastExecutionDateUtc { get; init; }
    public DateTime? NextExecutionDateUtc { get; init; }

    // Personnel & Audit Context
    public Guid? AssignedTechnicianId { get; init; }
    public string? AssignedTechnicianName { get; init; }
    public Guid CreatedByUserId { get; init; }
    public string CreatedByUserName { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;


    public DateTime ScheduledDateUtc
    {
        get => ScheduledStartDateUtc;
        init => ScheduledStartDateUtc = value;
    }

    public string Status { get; init; } = "Scheduled";
    public int? RecurringIntervalDays { get; init; }
}