namespace Notrelix.Application.Features.Identity.OAuth.Commands.StartOAuthLink;

public sealed class StartOAuthLinkCommandValidator : AbstractValidator<StartOAuthLinkCommand>
{
    public StartOAuthLinkCommandValidator()
    {
        RuleFor(x => x.Provider)
            .IsInEnum()
            .WithMessage("A valid OAuth provider is required.");
    }
}
