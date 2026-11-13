using FluentValidation;
using LabTrack.Application.Commands.Auth;

namespace LabTrack.Application.Validators.Auth;

public class LoginRequestValidator : AbstractValidator<LoginCommand>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.UsernameOrEmail)
            .NotEmpty()
            .WithMessage("Username or email is required.")
            .MaximumLength(256)
            .WithMessage("Username or email must not exceed 256 characters.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required.");
    }
}