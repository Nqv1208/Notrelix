namespace Notrelix.Application.Common.Oidc;

/// <summary>
/// Reads an OpenID Connect provider's discovery document (<c>{issuer}/.well-known/openid-configuration</c>) through a
/// guarded outbound path: https only, no redirect, a short timeout and a size cap, the host's addresses resolved and
/// refused when they are loopback, private, link-local (metadata included) or otherwise internal, and the connection
/// pinned to the addresses that were checked. The answer is a document or a stable reason, never an exception and
/// never provider output.
/// </summary>
public interface IOidcDiscoveryClient
{
    Task<OidcDiscoveryResult> DiscoverAsync(string issuer, CancellationToken cancellationToken);
}

/// <summary>The endpoints the sign-in flow needs, all https, from a document whose <c>issuer</c> equals the configured one.</summary>
public sealed record OidcDiscoveryDocument(string Issuer, string AuthorizationEndpoint, string TokenEndpoint, string JwksUri);

/// <summary>Either a <see cref="Document"/> or a stable <see cref="Failure"/> reason (see <see cref="OidcDiscoveryFailure"/>).</summary>
public sealed record OidcDiscoveryResult(OidcDiscoveryDocument? Document, string? Failure)
{
    public bool Succeeded => Document is not null;

    public static OidcDiscoveryResult Ok(OidcDiscoveryDocument document) => new(document, null);

    public static OidcDiscoveryResult Fail(string failure) => new(null, failure);
}

/// <summary>The stable reasons a discovery document is refused. They are shown to the Owner as the validation error.</summary>
public static class OidcDiscoveryFailure
{
    public const string IssuerInvalid = "issuer-invalid";
    public const string NotHttps = "not-https";
    public const string AddressRefused = "address-refused";
    public const string Unreachable = "unreachable";
    public const string Timeout = "timeout";
    public const string TooLarge = "too-large";
    public const string Redirected = "redirected";
    public const string BadStatus = "bad-status";
    public const string DocumentInvalid = "document-invalid";
    public const string IssuerMismatch = "issuer-mismatch";
    public const string EndpointInvalid = "endpoint-invalid";
}
