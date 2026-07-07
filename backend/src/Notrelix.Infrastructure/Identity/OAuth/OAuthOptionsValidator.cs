namespace Notrelix.Infrastructure.Identity.OAuth;

public sealed class OAuthOptionsValidator : IValidateOptions<OAuthOptions>
{
    public ValidateOptionsResult Validate(string? name, OAuthOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.FrontendSuccessUrl))
            return ValidateOptionsResult.Fail("OAuth:FrontendSuccessUrl is required.");

        if (string.IsNullOrWhiteSpace(options.FrontendFailureUrl))
            return ValidateOptionsResult.Fail("OAuth:FrontendFailureUrl is required.");

        foreach (var (key, config) in options.Providers)
        {
            if (!config.Enabled)
                continue;

            if (string.IsNullOrWhiteSpace(config.ClientId))
                return ValidateOptionsResult.Fail($"OAuth provider '{key}': ClientId is required when enabled.");

            if (string.IsNullOrWhiteSpace(config.ClientSecret))
                return ValidateOptionsResult.Fail($"OAuth provider '{key}': ClientSecret is required when enabled.");

            if (string.IsNullOrWhiteSpace(config.AuthorizationEndpoint))
                return ValidateOptionsResult.Fail($"OAuth provider '{key}': AuthorizationEndpoint is required.");

            if (string.IsNullOrWhiteSpace(config.TokenEndpoint))
                return ValidateOptionsResult.Fail($"OAuth provider '{key}': TokenEndpoint is required.");

            if (string.IsNullOrWhiteSpace(config.RedirectUri))
                return ValidateOptionsResult.Fail($"OAuth provider '{key}': RedirectUri is required.");

            if (config.Scopes.Length == 0)
                return ValidateOptionsResult.Fail($"OAuth provider '{key}': at least one Scope is required.");
        }

        return ValidateOptionsResult.Success;
    }
}
