using FluentValidation;

namespace LabTrack.Application.Dtos.Qr;

public sealed record GenerateQrDto
{
    public string TokenType { get; init; } = "WorkstationLogin";
    public Guid ReferenceId { get; init; }
    public int ExpirationMinutes { get; init; } = 15;
    public string? AdditionalPayload { get; init; }

    public string EntityType
    {
        get => TokenType;
        init => TokenType = value;
    }

    public Guid EntityId
    {
        get => ReferenceId;
        init => ReferenceId = value;
    }
}

public sealed class GenerateQrDtoValidator : AbstractValidator<GenerateQrDto>
{
    public GenerateQrDtoValidator()
    {
        RuleFor(x => x.TokenType)
            .NotEmpty().WithMessage("Token type is required.")
            .Must(t => t is "WorkstationLogin" or "StudentAttendance" or "QuickAuth")
            .WithMessage("TokenType must be WorkstationLogin, StudentAttendance, or QuickAuth.");

        RuleFor(x => x.ReferenceId)
            .NotEmpty().WithMessage("Reference ID is required.");

        RuleFor(x => x.ExpirationMinutes)
            .InclusiveBetween(1, 1440)
            .WithMessage("Expiration time must be between 1 minute and 24 hours (1440 minutes).");

        RuleFor(x => x.AdditionalPayload)
            .MaximumLength(500).WithMessage("Additional payload must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.AdditionalPayload));
    }
}