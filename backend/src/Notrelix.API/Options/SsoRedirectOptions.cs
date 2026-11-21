namespace Notrelix.API.Options;

/// <summary>
/// Where this deployment's API is reachable from the company's identity provider. The single sign-on callback is one
/// address for every provider: <c>{PublicApiBaseUrl}/api/v1/auth/sso/callback</c>. When unset the address is built from the
/// request (development).
/// </summary>
public sealed class SsoRedirectOptions
{
    public const string CallbackPath = "/api/v1/auth/sso/callback";

    public string? PublicApiBaseUrl { get; init; }

    public string CallbackUrl(HttpRequest request)
    {
        var baseUrl = string.IsNullOrWhiteSpace(PublicApiBaseUrl) ? $"{request.Scheme}://{request.Host}" : PublicApiBaseUrl.TrimEnd('/');
        return baseUrl + CallbackPath;
    }
}
