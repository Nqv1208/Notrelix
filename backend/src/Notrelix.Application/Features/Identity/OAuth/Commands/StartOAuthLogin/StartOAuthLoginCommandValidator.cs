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
            .WithMessage("ReturnUrl must be a relative path.");
    }

    private static bool BeSafeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
            return true;

        return returnUrl.StartsWith('/') && !returnUrl.Contains("//");
    }
}
