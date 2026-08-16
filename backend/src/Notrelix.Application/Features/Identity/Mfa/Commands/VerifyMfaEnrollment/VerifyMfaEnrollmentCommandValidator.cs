namespace Notrelix.Application.Features.Identity.Mfa.Commands.VerifyMfaEnrollment;

public class VerifyMfaEnrollmentCommandValidator : AbstractValidator<VerifyMfaEnrollmentCommand>
{
    public VerifyMfaEnrollmentCommandValidator()
    {
        RuleFor(x => x.MfaMethodId)
            .NotEmpty();

        RuleFor(x => x.Code)
            .NotEmpty()
            .Matches("^[0-9]{6}$");
    }
}
