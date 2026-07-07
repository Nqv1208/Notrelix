using FluentValidation;

namespace Notrelix.Application.Features.Identity.OAuth.Commands.CompleteOAuthLogin;

public class CompleteOAuthLoginCommandValidator : AbstractValidator<CompleteOAuthLoginCommand>
{
    public CompleteOAuthLoginCommandValidator()
    {
        RuleFor(x => x.Provider)
            .IsInEnum();

        RuleFor(x => x.Code)
            .NotEmpty()
            .When(x => string.IsNullOrWhiteSpace(x.Error));

        RuleFor(x => x.State)
            .NotEmpty()
            .When(x => string.IsNullOrWhiteSpace(x.Error));
    }
}
