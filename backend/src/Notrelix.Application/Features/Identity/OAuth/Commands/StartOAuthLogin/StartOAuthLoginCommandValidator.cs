namespace Notrelix.Application.Features.Identity.OAuth.Commands.StartOAuthLogin;

public class StartOAuthLoginCommandValidator : AbstractValidator<StartOAuthLoginCommand>
{
    public StartOAuthLoginCommandValidator()
    {
        RuleFor(x => x.Provider)
            .IsInEnum();

        RuleFor(x => x.ReturnUrl)
            .Must(BeSafeReturnUrl)
            .When(x => !string.IsNullOrWhiteSpace(x.ReturnUrl))
            .WithMessage("ReturnUrl must be an internal path that starts with a single slash.");
    }

    private static bool BeSafeReturnUrl(string? returnUrl) => SafeReturnPath.IsSafe(returnUrl);
}
