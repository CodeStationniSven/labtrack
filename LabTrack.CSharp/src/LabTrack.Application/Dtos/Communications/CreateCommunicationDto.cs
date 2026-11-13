using FluentValidation;

namespace LabTrack.Application.Dtos.Communications;

public sealed record CreateCommunicationDto
{
    public string Subject { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string Priority { get; init; } = "Normal"; // e.g., Low, Normal, High, Urgent
    public string Type { get; init; } = "Announcement"; // e.g., Announcement, Alert, MaintenanceNotice, DirectMessage
    public bool IsBroadcast { get; init; } = true;

    // Optional Targeting Scope
    public Guid? TargetLaboratoryId { get; init; }
    public string? TargetRole { get; init; } // e.g., All, Students, Faculty, LabStaff
    public Guid? RecipientUserId { get; init; }

    // Optional Expiration
    public DateTime? ExpiresAtUtc { get; init; }
}

public sealed class CreateCommunicationDtoValidator : AbstractValidator<CreateCommunicationDto>
{
    public CreateCommunicationDtoValidator()
    {
        RuleFor(x => x.Subject)
            .NotEmpty().WithMessage("Subject is required.")
            .MaximumLength(200).WithMessage("Subject must not exceed 200 characters.");

        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Message body is required.")
            .MaximumLength(4000).WithMessage("Message must not exceed 4000 characters.");

        RuleFor(x => x.Priority)
            .NotEmpty().WithMessage("Priority is required.")
            .Must(p => p is "Low" or "Normal" or "High" or "Urgent")
            .WithMessage("Priority must be Low, Normal, High, or Urgent.");

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Communication type is required.")
            .Must(t => t is "Announcement" or "Alert" or "MaintenanceNotice" or "DirectMessage")
            .WithMessage("Type must be Announcement, Alert, MaintenanceNotice, or DirectMessage.");

        RuleFor(x => x.TargetRole)
            .MaximumLength(50).WithMessage("Target role must not exceed 50 characters.")
            .When(x => !string.IsNullOrEmpty(x.TargetRole));

        RuleFor(x => x.RecipientUserId)
            .NotEmpty().WithMessage("Recipient user ID is required for non-broadcast direct messages.")
            .When(x => !x.IsBroadcast && x.Type == "DirectMessage");

        RuleFor(x => x.ExpiresAtUtc)
            .GreaterThan(DateTime.UtcNow).WithMessage("Expiration date must be in the future.")
            .When(x => x.ExpiresAtUtc.HasValue);
    }
}