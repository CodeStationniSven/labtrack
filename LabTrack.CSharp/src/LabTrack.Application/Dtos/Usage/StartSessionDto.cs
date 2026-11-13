using FluentValidation;

namespace LabTrack.Application.Dtos.Usage;

public sealed record StartSessionDto
{
    public Guid WorkstationId { get; init; }
    public Guid StudentId { get; init; }
    public Guid LaboratoryId { get; init; }
    public string AuthenticationMethod { get; init; } = "QrCode"; // e.g., QrCode, Manual, NFC
    public string? QrToken { get; init; }
    public string? ClientIpAddress { get; init; }
    public DateTime StartedAtUtc { get; init; } = DateTime.UtcNow;
}

public sealed class StartSessionDtoValidator : AbstractValidator<StartSessionDto>
{
    public StartSessionDtoValidator()
    {
        RuleFor(x => x.WorkstationId)
            .NotEmpty().WithMessage("Workstation ID is required.");

        RuleFor(x => x.StudentId)
            .NotEmpty().WithMessage("Student ID is required.");

        RuleFor(x => x.LaboratoryId)
            .NotEmpty().WithMessage("Laboratory ID is required.");

        RuleFor(x => x.AuthenticationMethod)
            .NotEmpty().WithMessage("Authentication method is required.")
            .Must(m => m is "QrCode" or "Manual" or "NFC")
            .WithMessage("Authentication method must be QrCode, Manual, or NFC.");

        RuleFor(x => x.QrToken)
            .NotEmpty().WithMessage("QR Token is required when authentication method is QrCode.")
            .When(x => x.AuthenticationMethod == "QrCode");

        RuleFor(x => x.ClientIpAddress)
            .MaximumLength(45).WithMessage("IP address length must not exceed 45 characters.")
            .When(x => !string.IsNullOrEmpty(x.ClientIpAddress));

        RuleFor(x => x.StartedAtUtc)
            .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5))
            .WithMessage("Session start timestamp cannot be in the future.");
    }
}