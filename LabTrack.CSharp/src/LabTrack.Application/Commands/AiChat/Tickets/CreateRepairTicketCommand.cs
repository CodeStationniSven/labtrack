using MediatR;

namespace LabTrack.Application.Commands.Tickets;

public sealed record CreateRepairTicketCommand : IRequest<Guid>
{
    public Guid LaboratoryId { get; init; }
    public Guid WorkstationId { get; init; }
    public Guid? AssetId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string IssueDescription { get; init; } = string.Empty;
    public string Category { get; init; } = "Hardware";
    public string Priority { get; init; } = "Medium";
    public string? StepsToReproduce { get; init; }
    public string? AttachmentUrl { get; init; }

    public string ProblemDescription
    {
        get => IssueDescription;
        init => IssueDescription = value;
    }

    public Guid ReportedByUserId { get; init; }
}