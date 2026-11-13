using FluentValidation;

namespace LabTrack.Application.Dtos.Usage;

public sealed record HeartbeatDto
{
    public Guid SessionId { get; init; }
    public Guid WorkstationId { get; init; }
    public Guid StudentId { get; init; }

    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
    public string? SystemStatus { get; init; } = "Active"; // e.g., Active, Idle, Locked
    public double? CpuUsagePercentage { get; init; }
    public double? MemoryUsagePercentage { get; init; }
    public string? ClientIpAddress { get; init; }
}

public sealed class HeartbeatDtoValidator : AbstractValidator<HeartbeatDto>
{
    public HeartbeatDtoValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty().WithMessage("Session ID is required.");

        RuleFor(x => x.WorkstationId)
            .NotEmpty().WithMessage("Workstation ID is required.");

        RuleFor(x => x.StudentId)
            .NotEmpty().WithMessage("Student ID is required.");

        RuleFor(x => x.TimestampUtc)
            .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5))
            .WithMessage("Heartbeat timestamp cannot be in the future.");

        RuleFor(x => x.SystemStatus)
            .Must(s => s is null or "Active" or "Idle" or "Locked")
            .WithMessage("System status must be Active, Idle, or Locked.");

        RuleFor(x => x.CpuUsagePercentage)
            .InclusiveBetween(0, 100)
            .WithMessage("CPU usage percentage must be between 0 and 100.")
            .When(x => x.CpuUsagePercentage.HasValue);

        RuleFor(x => x.MemoryUsagePercentage)
            .InclusiveBetween(0, 100)
            .WithMessage("Memory usage percentage must be between 0 and 100.")
            .When(x => x.MemoryUsagePercentage.HasValue);

        RuleFor(x => x.ClientIpAddress)
            .MaximumLength(45).WithMessage("IP address length must not exceed 45 characters.")
            .When(x => !string.IsNullOrEmpty(x.ClientIpAddress));
    }
}