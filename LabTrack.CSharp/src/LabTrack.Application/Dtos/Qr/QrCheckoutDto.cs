using FluentValidation;

namespace LabTrack.Application.Dtos.Qr;

public sealed record QrCheckoutDto
{
    public Guid SessionId { get; init; }
    public Guid WorkstationId { get; init; }
    public Guid StudentId { get; init; }
    public string? Token { get; init; }
    public DateTime CheckedOutAtUtc { get; init; } = DateTime.UtcNow;
    public string? Notes { get; init; }
}

public sealed class QrCheckoutDtoValidator : AbstractValidator<QrCheckoutDto>
{
    public QrCheckoutDtoValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty().WithMessage("Session ID is required.");

        RuleFor(x => x.WorkstationId)
            .NotEmpty().WithMessage("Workstation ID is required.");

        RuleFor(x => x.StudentId)
            .NotEmpty().WithMessage("Student ID is required.");

        RuleFor(x => x.Token)
            .MaximumLength(1024).WithMessage("QR Token length must not exceed 1024 characters.")
            .When(x => !string.IsNullOrEmpty(x.Token));

        RuleFor(x => x.CheckedOutAtUtc)
            .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5))
            .WithMessage("Checkout timestamp cannot be in the future.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}