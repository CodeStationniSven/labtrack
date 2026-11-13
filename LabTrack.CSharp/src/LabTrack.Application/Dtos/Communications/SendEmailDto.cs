using FluentValidation;

namespace LabTrack.Application.Dtos.Communications;

public sealed record SendEmailDto
{
    public string ToEmail { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public bool IsHtml { get; init; } = true;

    // Optional Attachments / Carbon Copy
    public IReadOnlyCollection<string>? CcEmails { get; init; }
    public IReadOnlyCollection<string>? BccEmails { get; init; }
}

public sealed class SendEmailDtoValidator : AbstractValidator<SendEmailDto>
{
    public SendEmailDtoValidator()
    {
        RuleFor(x => x.ToEmail)
            .NotEmpty().WithMessage("Recipient email address is required.")
            .EmailAddress().WithMessage("A valid recipient email address is required.")
            .MaximumLength(256).WithMessage("Email address must not exceed 256 characters.");

        RuleFor(x => x.Subject)
            .NotEmpty().WithMessage("Email subject is required.")
            .MaximumLength(200).WithMessage("Subject must not exceed 200 characters.");

        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("Email body is required.");

        RuleForEach(x => x.CcEmails)
            .EmailAddress().WithMessage("CC email address must be a valid email format.")
            .MaximumLength(256).WithMessage("CC email address must not exceed 256 characters.")
            .When(x => x.CcEmails != null && x.CcEmails.Any());

        RuleForEach(x => x.BccEmails)
            .EmailAddress().WithMessage("BCC email address must be a valid email format.")
            .MaximumLength(256).WithMessage("BCC email address must not exceed 256 characters.")
            .When(x => x.BccEmails != null && x.BccEmails.Any());
    }
}