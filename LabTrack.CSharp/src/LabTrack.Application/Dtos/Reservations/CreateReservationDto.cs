using FluentValidation;

namespace LabTrack.Application.Dtos.Reservations;

public sealed record CreateReservationDto
{
    public Guid UserId { get; init; }

    public Guid LaboratoryId { get; init; }
    public Guid? WorkstationId { get; init; }

    public string Title { get; init; } = string.Empty;
    public string Purpose { get; init; } = string.Empty;
    public string ReservationType { get; init; } = "ClassUse";

    public DateTime StartTimeUtc { get; init; }
    public DateTime EndTimeUtc { get; init; }

    public int ExpectedAttendees { get; init; } = 1;
    public string? SpecialRequirements { get; init; }
}

public sealed class CreateReservationDtoValidator : AbstractValidator<CreateReservationDto>
{
    public CreateReservationDtoValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.LaboratoryId)
            .NotEmpty().WithMessage("Laboratory ID is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Reservation title is required.")
            .MaximumLength(150).WithMessage("Title must not exceed 150 characters.");

        RuleFor(x => x.Purpose)
            .NotEmpty().WithMessage("Reservation purpose is required.")
            .MaximumLength(1000).WithMessage("Purpose must not exceed 1000 characters.");

        RuleFor(x => x.ReservationType)
            .NotEmpty().WithMessage("Reservation type is required.")
            .Must(t => t is "ClassUse" or "IndividualStudy" or "Maintenance" or "Research" or "Exam")
            .WithMessage("ReservationType must be ClassUse, IndividualStudy, Maintenance, Research, or Exam.");

        RuleFor(x => x.StartTimeUtc)
            .GreaterThanOrEqualTo(DateTime.UtcNow.AddMinutes(-5))
            .WithMessage("Reservation start time cannot be in the past.");

        RuleFor(x => x.EndTimeUtc)
            .GreaterThan(x => x.StartTimeUtc)
            .WithMessage("Reservation end time must be after the start time.");

        RuleFor(x => x)
            .Must(x => (x.EndTimeUtc - x.StartTimeUtc).TotalHours <= 12)
            .WithMessage("A single reservation duration cannot exceed 12 hours.");

        RuleFor(x => x.ExpectedAttendees)
            .GreaterThan(0).WithMessage("Expected attendees count must be at least 1.")
            .LessThanOrEqualTo(200).WithMessage("Expected attendees count must not exceed 200.");

        RuleFor(x => x.SpecialRequirements)
            .MaximumLength(500).WithMessage("Special requirements must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.SpecialRequirements));
    }
}