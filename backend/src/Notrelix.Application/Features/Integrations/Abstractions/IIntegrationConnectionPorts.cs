namespace Notrelix.Application.Features.Integrations.Abstractions;

/// <summary>Which providers have a client id and secret in configuration (`Integrations:{Provider}:ClientId`/`ClientSecret`).</summary>
public interface IIntegrationProviderSettings
{
    bool IsConfigured(IntegrationProvider provider);
}

public sealed record IntegrationOAuthTokens(string AccessToken, string? RefreshToken, string? ProviderAccountId, IReadOnlyList<string> Scopes);

/// <summary>The provider side of the OAuth redirect flow. Tokens never leave the backend.</summary>
public interface IIntegrationOAuthClient
{
    string BuildAuthorizeUrl(IntegrationProvider provider, string redirectUri, string state);

    /// <summary>Redeems the authorization code; null when the provider refuses it.</summary>
    Task<IntegrationOAuthTokens?> ExchangeCodeAsync(
        IntegrationProvider provider, string code, string redirectUri, CancellationToken cancellationToken);
}

public sealed record IntegrationOAuthState(
    string ProviderKey, Guid WorkspaceId, Guid UserId, string RedirectUri);

/// <summary>The `state` of the redirect flow: signed and short-lived, so the callback knows who started it and where.</summary>
public interface IIntegrationOAuthStateProtector
{
    string Protect(IntegrationOAuthState state);

    IntegrationOAuthState? Unprotect(string protectedState);
}

/// <summary>Whether an outgoing webhook may deliver to an address (https only and no private network outside development).</summary>
public interface IWebhookTargetPolicy
{
    /// <summary>Null when the address is acceptable, otherwise the reason it is refused.</summary>
    string? Validate(string url);
}
