namespace Notrelix.Application.Features.Identity.Auth.Commands.ResetPassword;

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Verification code is required")
            .Length(6).WithMessage("Code must be 6 digits")
            .Matches(@"^\d{6}$").WithMessage("Code must contain only digits");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required")
            .MinimumLength(PasswordPolicy.MinimumLength)
                .WithMessage($"Password must be at least {PasswordPolicy.MinimumLength} characters");
    }
}
