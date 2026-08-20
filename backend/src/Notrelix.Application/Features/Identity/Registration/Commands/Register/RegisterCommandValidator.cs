namespace Notrelix.Application.Features.Identity.Registration.Commands.Register;

// Validator cho RegisterCommand
public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(256).EmailAddress();
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(PasswordPolicy.MinimumLength)
                .WithMessage($"Password must be at least {PasswordPolicy.MinimumLength} characters");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
