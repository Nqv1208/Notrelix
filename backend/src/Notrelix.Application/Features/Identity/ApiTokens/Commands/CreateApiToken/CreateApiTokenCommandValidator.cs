namespace Notrelix.Application.Features.Identity.ApiTokens.Commands.CreateApiToken;

/// <summary>
/// P12-CLEANUP-006 — validate the request before any step-up proof is consumed.
/// Name must be a non-empty trimmed value within the token-name limit; the
/// step-up proof must be present; expiration must be in the future.
/// </summary>
public class CreateApiTokenCommandValidator : AbstractValidator<CreateApiTokenCommand>
{
    public CreateApiTokenCommandValidator(IDateTimeProvider dateTimeProvider)
    {
        RuleFor(x => x.Name)
            .Must(name => !string.IsNullOrWhiteSpace(name)
                          && name.Trim().Length <= CreateApiTokenCommandHandler.MaxTokenNameLength)
            .WithErrorCode("identity.api-tokens.invalid-name")
            .WithMessage($"Token name must be between 1 and {CreateApiTokenCommandHandler.MaxTokenNameLength} characters.");

        RuleFor(x => x.StepUpToken)
            .NotEmpty()
            .WithErrorCode("identity.api-tokens.invalid-step-up-token")
            .WithMessage("Step-up verification token is required.");

        RuleFor(x => x.ExpiresAt)
            .Must(expiresAt => !expiresAt.HasValue || expiresAt.Value > dateTimeProvider.UtcNow)
            .WithErrorCode("identity.api-tokens.invalid-expiration")
            .WithMessage("Token expiration must be in the future.");
    }
}
