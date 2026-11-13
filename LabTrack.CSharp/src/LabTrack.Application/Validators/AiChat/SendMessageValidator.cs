using FluentValidation;
using LabTrack.Application.Commands.AiChat;

namespace LabTrack.Application.Validators.AiChat;

public class SendMessageValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageValidator()
    {
        RuleFor(x => x.Question)
            .NotEmpty()
            .WithMessage("Question cannot be empty.")
            .MaximumLength(2000)
            .WithMessage("Question cannot exceed 2000 characters.");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required.");
    }
}