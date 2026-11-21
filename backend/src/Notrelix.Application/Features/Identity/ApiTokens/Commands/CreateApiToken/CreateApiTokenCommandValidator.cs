using Notrelix.Domain.Identity.Tokens;

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

        // At least one scope, each from the published vocabulary: none would mean no access, never full access.
        RuleFor(x => x.Scopes)
            .Must(scopes => scopes is { Count: > 0 })
            .WithErrorCode("identity.api-tokens.scopes-required")
            .WithMessage("An API token needs at least one scope.");

        RuleFor(x => x.Scopes)
            .Must(scopes => scopes is null || scopes.All(ApiTokenScopes.Vocabulary.Contains))
            .WithErrorCode("identity.api-tokens.unknown-scope")
            .WithMessage(x => $"Unknown API token scope: {string.Join(", ", (x.Scopes ?? []).Where(s => !ApiTokenScopes.Vocabulary.Contains(s)).Order())}.");

        // An API token needs an expiry, within the default maximum lifetime (Lead decision L7).
        RuleFor(x => x.ExpiresAt)
            .NotNull()
            .WithErrorCode("identity.api-tokens.expiration-required")
            .WithMessage("An API token needs an expiration date.");

        RuleFor(x => x.ExpiresAt)
            .Must(expiresAt => !expiresAt.HasValue || expiresAt.Value > dateTimeProvider.UtcNow)
            .WithErrorCode("identity.api-tokens.invalid-expiration")
            .WithMessage("Token expiration must be in the future.");

        RuleFor(x => x.ExpiresAt)
            .Must(expiresAt => !expiresAt.HasValue
                               || expiresAt.Value <= dateTimeProvider.UtcNow.AddDays(CreateApiTokenCommandHandler.MaxTokenLifetimeDays))
            .WithErrorCode("identity.api-tokens.lifetime-exceeded")
            .WithMessage($"An API token cannot live longer than {CreateApiTokenCommandHandler.MaxTokenLifetimeDays} days.");
    }
}
