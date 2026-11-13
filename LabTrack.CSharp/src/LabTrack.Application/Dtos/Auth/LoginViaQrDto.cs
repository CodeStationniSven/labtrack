using FluentValidation;

namespace LabTrack.Application.Dtos.Auth;

public sealed record LoginViaQrDto
{
    /// <summary>
    /// The encrypted or HMAC-signed payload extracted from the student's scanned QR code.
    /// </summary>
    public string QrToken { get; init; } = string.Empty;

    /// <summary>
    /// The unique workstation or PC identifier where the QR scan was initiated.
    /// </summary>
    public Guid WorkstationId { get; init; }

    /// <summary>
    /// Optional IP address or hardware address of the scanning terminal/station.
    /// </summary>
    public string? TerminalIpAddress { get; init; }
}

public sealed class LoginViaQrDtoValidator : AbstractValidator<LoginViaQrDto>
{
    public LoginViaQrDtoValidator()
    {
        RuleFor(x => x.QrToken)
            .NotEmpty().WithMessage("QR token payload is required.")
            .MinimumLength(16).WithMessage("Invalid or malformed QR token payload.");

        RuleFor(x => x.WorkstationId)
            .NotEmpty().WithMessage("Workstation ID is required to identify the target PC/station.");

        RuleFor(x => x.TerminalIpAddress)
            .MaximumLength(45).WithMessage("IP address must not exceed 45 characters.")
            .When(x => !string.IsNullOrEmpty(x.TerminalIpAddress));
    }
}