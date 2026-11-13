using FluentValidation;

namespace LabTrack.Application.Dtos.Users;

public sealed record UpdateUserDto
{
    public Guid UserId { get; init; }
    public string SchoolId { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Role { get; init; } = "Student";
    public Guid? AssignedLaboratoryId { get; init; }
    public string? Department { get; init; }
    public bool IsActive { get; init; } = true;

    public string Username
    {
        get => SchoolId;
        init => SchoolId = value;
    }

    public IReadOnlyList<string> Roles
    {
        get => new[] { Role };
        init => Role = value?.FirstOrDefault() ?? "Student";
    }
}

public sealed class UpdateUserDtoValidator : AbstractValidator<UpdateUserDto>
{
    public UpdateUserDtoValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.SchoolId)
            .NotEmpty().WithMessage("School ID / Student ID is required.")
            .MaximumLength(50).WithMessage("School ID must not exceed 50 characters.");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email address is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(256).WithMessage("Email address must not exceed 256 characters.");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("User role is required.")
            .Must(role => role is "Student" or "Instructor" or "Technician" or "Admin" or "LabInCharge")
            .WithMessage("Role must be Student, Instructor, Technician, Admin, or LabInCharge.");

        RuleFor(x => x.Department)
            .MaximumLength(100).WithMessage("Department name must not exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.Department));
    }
}