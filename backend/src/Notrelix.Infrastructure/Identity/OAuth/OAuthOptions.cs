using Notrelix.Domain.Identity.OAuth;

namespace Notrelix.Infrastructure.Identity.OAuth;

public sealed class OAuthOptions
{
    public string FrontendSuccessUrl { get; init; } = "/auth/callback";
    public string FrontendFailureUrl { get; init; } = "/login";
    public Dictionary<string, OAuthProviderConfig> Providers { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public OAuthProviderConfig GetProviderConfig(OAuthProvider provider)
    {
        var key = provider.ToString();
        if (!Providers.TryGetValue(key, out var config))
            throw new InvalidOperationException($"OAuth provider '{key}' is not configured.");

        if (!config.Enabled)
            throw new InvalidOperationException($"OAuth provider '{key}' is disabled.");

        return config;
    }
}

public sealed class OAuthProviderConfig
{
    public bool Enabled { get; init; }
    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
    public string AuthorizationEndpoint { get; init; } = string.Empty;
    public string TokenEndpoint { get; init; } = string.Empty;
    public string UserInfoEndpoint { get; init; } = string.Empty;
    public string? EmailsEndpoint { get; init; }
    public string? JwksUri { get; init; }
    public string? Issuer { get; init; }
    public string RedirectUri { get; init; } = string.Empty;
    public string[] Scopes { get; init; } = [];
}
