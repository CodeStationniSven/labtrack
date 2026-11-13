using FluentValidation;

namespace LabTrack.Application.Dtos.Qr;

public sealed record QrCheckinDto
{
    public string Token { get; init; } = string.Empty;
    public Guid WorkstationId { get; init; }
    public Guid StudentId { get; init; }
    public DateTime ScannedAtUtc { get; init; } = DateTime.UtcNow;
    public string? ScannedFromIp { get; init; }
}

public sealed class QrCheckinDtoValidator : AbstractValidator<QrCheckinDto>
{
    public QrCheckinDtoValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("QR Token string is required.")
            .MaximumLength(1024).WithMessage("QR Token length must not exceed 1024 characters.");

        RuleFor(x => x.WorkstationId)
            .NotEmpty().WithMessage("Workstation ID is required.");

        RuleFor(x => x.StudentId)
            .NotEmpty().WithMessage("Student ID is required.");

        RuleFor(x => x.ScannedAtUtc)
            .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5))
            .WithMessage("Scan timestamp cannot be in the future.");

        RuleFor(x => x.ScannedFromIp)
            .MaximumLength(45).WithMessage("IP address length must not exceed 45 characters.")
            .When(x => !string.IsNullOrEmpty(x.ScannedFromIp));
    }
}