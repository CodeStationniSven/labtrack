using FluentValidation;

namespace LabTrack.Application.Dtos.Auth;

public sealed record SetPinDto
{
    public Guid UserId { get; init; }
    public string Pin { get; init; } = string.Empty;
    public string ConfirmPin { get; init; } = string.Empty;
}

public sealed class SetPinDtoValidator : AbstractValidator<SetPinDto>
{
    public SetPinDtoValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.Pin)
            .NotEmpty().WithMessage("PIN is required.")
            .Matches(@"^\d{4,6}$").WithMessage("PIN must be between 4 and 6 numeric digits.");

        RuleFor(x => x.ConfirmPin)
            .NotEmpty().WithMessage("Confirmation PIN is required.")
            .Equal(x => x.Pin).WithMessage("PIN and confirmation PIN do not match.");
    }
}