namespace Notrelix.Application.Features.Identity.OAuth.Commands.UnlinkOAuth;

public sealed class UnlinkOAuthCommandValidator : AbstractValidator<UnlinkOAuthCommand>
{
    public UnlinkOAuthCommandValidator()
    {
        RuleFor(x => x.Provider)
            .IsInEnum()
            .WithMessage("A valid OAuth provider is required.");

        RuleFor(x => x.StepUpToken)
            .NotEmpty()
            .WithMessage("Strong verification is required for this action.");
    }
}
