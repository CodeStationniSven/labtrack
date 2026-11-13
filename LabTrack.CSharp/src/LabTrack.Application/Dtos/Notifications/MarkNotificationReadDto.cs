using FluentValidation;

namespace LabTrack.Application.Dtos.Notifications;

public sealed record MarkNotificationReadDto
{
    public Guid NotificationId { get; init; }
    public Guid UserId { get; init; }
}

public sealed class MarkNotificationReadDtoValidator : AbstractValidator<MarkNotificationReadDto>
{
    public MarkNotificationReadDtoValidator()
    {
        RuleFor(x => x.NotificationId)
            .NotEmpty().WithMessage("Notification ID is required.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");
    }
}