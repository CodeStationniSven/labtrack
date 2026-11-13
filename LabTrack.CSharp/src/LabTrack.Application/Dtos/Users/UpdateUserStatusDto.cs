using FluentValidation;

namespace LabTrack.Application.Dtos.Users;

public sealed record UpdateUserStatusDto
{
    public Guid UserId { get; init; }
    public bool IsActive { get; init; }
    public string SystemStatus { get; init; } = "Active"; // e.g., Active, Suspended, Inactive, Archived
    public string? Reason { get; init; }
}

public sealed class UpdateUserStatusDtoValidator : AbstractValidator<UpdateUserStatusDto>
{
    public UpdateUserStatusDtoValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.SystemStatus)
            .NotEmpty().WithMessage("System status is required.")
            .Must(s => s is "Active" or "Suspended" or "Inactive" or "Archived")
            .WithMessage("System status must be Active, Suspended, Inactive, or Archived.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reason is required when suspending or archiving a user account.")
            .When(x => x.SystemStatus is "Suspended" or "Archived")
            .MaximumLength(500).WithMessage("Reason must not exceed 500 characters.");
    }
}