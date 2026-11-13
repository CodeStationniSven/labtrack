using FluentValidation;
using LabTrack.Application.Commands.Laboratories;

namespace LabTrack.Application.Validators.Laboratories;

public class CreateLaboratoryValidator : AbstractValidator<CreateLaboratoryCommand>
{
    public CreateLaboratoryValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Laboratory name is required.")
            .MaximumLength(150)
            .WithMessage("Laboratory name must not exceed 150 characters.");

        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage("Laboratory code is required.")
            .MaximumLength(50)
            .WithMessage("Laboratory code must not exceed 50 characters.");

        RuleFor(x => x.Location)
            .NotEmpty()
            .WithMessage("Location is required.")
            .MaximumLength(200)
            .WithMessage("Location must not exceed 200 characters.");

        RuleFor(x => x.Capacity)
            .GreaterThan(0)
            .WithMessage("Capacity must be greater than zero.");

        RuleFor(x => x.SupervisorId)
            .NotEmpty()
            .WithMessage("Supervisor ID is required.")
            .When(x => x.SupervisorId.HasValue);
    }
}