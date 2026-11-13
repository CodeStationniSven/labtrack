using FluentValidation;

namespace LabTrack.Application.Dtos.Usage;

public sealed record ScanLoginDto
{
    public string QrToken { get; init; } = string.Empty;
    public Guid WorkstationId { get; init; }
    public Guid StudentId { get; init; }
    public string? ClientIpAddress { get; init; }
    public DateTime ScannedAtUtc { get; init; } = DateTime.UtcNow;
}

public sealed class ScanLoginDtoValidator : AbstractValidator<ScanLoginDto>
{
    public ScanLoginDtoValidator()
    {
        RuleFor(x => x.QrToken)
            .NotEmpty().WithMessage("QR token is required.")
            .MaximumLength(1024).WithMessage("QR token length must not exceed 1024 characters.");

        RuleFor(x => x.WorkstationId)
            .NotEmpty().WithMessage("Workstation ID is required.");

        RuleFor(x => x.StudentId)
            .NotEmpty().WithMessage("Student ID is required.");

        RuleFor(x => x.ClientIpAddress)
            .MaximumLength(45).WithMessage("IP address length must not exceed 45 characters.")
            .When(x => !string.IsNullOrEmpty(x.ClientIpAddress));

        RuleFor(x => x.ScannedAtUtc)
            .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5))
            .WithMessage("Scan timestamp cannot be in the future.");
    }
}