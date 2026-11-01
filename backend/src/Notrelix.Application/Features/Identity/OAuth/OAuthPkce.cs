namespace Notrelix.Application.Features.Identity.OAuth;

/// <summary>
/// Which providers take a PKCE code challenge (S256) at authorization and the matching verifier at the
/// token endpoint. The start commands and the provider client read this one answer, so a provider
/// never gets a challenge it will not be asked to prove.
/// </summary>
public static class OAuthPkce
{
    public static bool IsUsedBy(OAuthProvider provider) =>
        provider is OAuthProvider.Google or OAuthProvider.Microsoft;
}
