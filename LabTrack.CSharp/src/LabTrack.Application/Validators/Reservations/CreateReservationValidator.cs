using FluentValidation;
using LabTrack.Application.Commands.Reservations;

namespace LabTrack.Application.Validators.Reservations;

public class CreateReservationValidator : AbstractValidator<CreateReservationCommand>
{
    public CreateReservationValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required.");

        RuleFor(x => x.LaboratoryId)
            .NotEmpty()
            .WithMessage("Laboratory ID is required.");

        RuleFor(x => x.WorkstationId)
            .NotEmpty()
            .WithMessage("Workstation ID is required.");

        RuleFor(x => x.StartTimeUtc)
            .GreaterThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("Reservation start time cannot be in the past.");

        RuleFor(x => x.EndTimeUtc)
            .GreaterThan(x => x.StartTimeUtc)
            .WithMessage("Reservation end time must be after the start time.");

        RuleFor(x => x.Purpose)
            .NotEmpty()
            .WithMessage("Reservation purpose is required.")
            .MaximumLength(500)
            .WithMessage("Purpose must not exceed 500 characters.");
    }
}