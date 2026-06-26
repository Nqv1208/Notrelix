using FluentValidation;

namespace Notrelix.Application.Features.Identity.Auth.Commands.Login;

// Validator cho LoginCommand
public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .MaximumLength(256)
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty();
    }
}
