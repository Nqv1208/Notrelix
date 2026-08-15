using FluentValidation;
using Notrelix.Domain.Identity.OAuth;

namespace Notrelix.Application.Features.Identity.OAuth.Commands.CompleteOAuthLink;

public sealed class CompleteOAuthLinkCommandValidator : AbstractValidator<CompleteOAuthLinkCommand>
{
    public CompleteOAuthLinkCommandValidator()
    {
        RuleFor(x => x.Provider)
            .IsInEnum()
            .WithMessage("A valid OAuth provider is required.");

        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage("OAuth code is required.");

        RuleFor(x => x.State)
            .NotEmpty()
            .WithMessage("OAuth state is required.");
    }
}
