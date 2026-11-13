using MediatR;

namespace LabTrack.Application.Commands.AiChat.Reports;

public sealed record CreateProblemReportCommand : IRequest<Guid>
{
    public Guid ReportedByUserId { get; init; }

    public Guid ReporterId
    {
        get => ReportedByUserId;
        init => ReportedByUserId = value;
    }

    public Guid LaboratoryId { get; init; }
    public Guid? WorkstationId { get; init; }
    public Guid? AssetId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Category { get; init; } = "Hardware";
    public string Severity { get; init; } = "Medium";
    public string Description { get; init; } = string.Empty;
    public string? StepsToReproduce { get; init; }
    public string? AttachmentUrl { get; init; }
    public DateTime ReportedAtUtc { get; init; } = DateTime.UtcNow;
}
