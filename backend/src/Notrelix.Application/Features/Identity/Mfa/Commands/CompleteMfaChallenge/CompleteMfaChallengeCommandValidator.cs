namespace Notrelix.Application.Features.Identity.Mfa.Commands.CompleteMfaChallenge;

public class CompleteMfaChallengeCommandValidator : AbstractValidator<CompleteMfaChallengeCommand>
{
    public CompleteMfaChallengeCommandValidator()
    {
        RuleFor(x => x.ChallengeToken)
            .NotEmpty();

        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(64);
    }
}
