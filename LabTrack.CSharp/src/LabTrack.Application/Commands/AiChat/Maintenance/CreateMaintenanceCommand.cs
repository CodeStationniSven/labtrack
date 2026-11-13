using MediatR;

namespace LabTrack.Application.Commands.Maintenance;

public sealed record CreateMaintenanceCommand : IRequest<Guid>
{
    public Guid LaboratoryId { get; init; }
    public Guid? WorkstationId { get; init; }
    public Guid? AssetId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string MaintenanceType { get; init; } = "Preventive";
    public string RecurrencePattern { get; init; } = "OneTime";
    public DateTime ScheduledStartDateUtc { get; init; }
    public DateTime? ScheduledEndDateUtc { get; init; }
    public Guid? AssignedTechnicianId { get; init; }

    public DateTime ScheduledDateUtc
    {
        get => ScheduledStartDateUtc;
        init => ScheduledStartDateUtc = value;
    }

    public int? RecurringIntervalDays { get; init; }
}