using FluentValidation;

namespace LabTrack.Application.Dtos.Laboratories;

public sealed record UpdateLaboratoryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Building { get; init; } = string.Empty;
    public string FloorLevel { get; init; } = string.Empty;
    public string RoomNumber { get; init; } = string.Empty;
    public int TotalWorkstations { get; init; }
    public bool IsActive { get; init; }
    public TimeSpan OpeningTime { get; init; }
    public TimeSpan ClosingTime { get; init; }
    public Guid? InChargeStaffId { get; init; }

    // ─── DINAGDAG ───
    public string? Location { get; init; }
    public int? Capacity { get; init; }
    public Guid? SupervisorId { get; init; }
    // ─── END DINAGDAG ───
}

public sealed class UpdateLaboratoryDtoValidator : AbstractValidator<UpdateLaboratoryDto>
{
    public UpdateLaboratoryDtoValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Laboratory ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Laboratory name is required.")
            .MaximumLength(150).WithMessage("Laboratory name must not exceed 150 characters.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Laboratory code is required.")
            .MaximumLength(30).WithMessage("Laboratory code must not exceed 30 characters.")
            .Matches(@"^[a-zA-Z0-9\-_]+$").WithMessage("Laboratory code can only contain letters, numbers, hyphens, and underscores.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Building)
            .NotEmpty().WithMessage("Building location is required.")
            .MaximumLength(100).WithMessage("Building name must not exceed 100 characters.");

        RuleFor(x => x.FloorLevel)
            .NotEmpty().WithMessage("Floor level is required.")
            .MaximumLength(20).WithMessage("Floor level must not exceed 20 characters.");

        RuleFor(x => x.RoomNumber)
            .NotEmpty().WithMessage("Room number is required.")
            .MaximumLength(30).WithMessage("Room number must not exceed 30 characters.");

        RuleFor(x => x.TotalWorkstations)
            .GreaterThan(0).WithMessage("Total workstations must be greater than zero.");

        RuleFor(x => x.ClosingTime)
            .GreaterThan(x => x.OpeningTime)
            .WithMessage("Closing time must be later than opening time.");
    }
}