using System.Net;
using Notrelix.Application.Features.Integrations.Connections;
using Notrelix.Infrastructure.Integrations.Connections;

namespace Notrelix.Infrastructure.Oidc;

/// <summary>Options of the guarded outbound path to company identity providers (configuration section <c>Oidc:Outbound</c>).</summary>
public sealed class OidcOutboundOptions
{
    public const string SectionName = "Oidc:Outbound";

    /// <summary>Lets the path reach loopback and private networks. Off unless a test or a development setup says so.</summary>
    public bool AllowPrivateTargets { get; init; }
}

/// <summary>
/// The one outbound path to a company identity provider (discovery document, token endpoint, JWKS): the named
/// HttpClient resolves the host at connect time, refuses loopback, private, link-local (metadata included) and other
/// internal addresses, connects only to the addresses it checked, follows no redirect and gives up after 5 seconds.
/// </summary>
public static class OidcGuardedHttp
{
    public const string ClientName = "oidc-outbound";

    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    internal static SocketsHttpHandler CreateHandler(bool allowPrivate) => new()
    {
        AllowAutoRedirect = false,
        UseProxy = false,
        ConnectTimeout = Timeout,
        ConnectCallback = (context, ct) => OutboundWebhookSender.ConnectAsync(context, allowPrivate, ct),
    };

    /// <summary>True when the address is https and its host is not an internal address given as a literal or as localhost.</summary>
    public static bool IsAllowedEndpoint(string? url, bool allowPrivate) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps
        && string.IsNullOrEmpty(uri.UserInfo)
        && !string.IsNullOrEmpty(uri.Host)
        && (allowPrivate || !uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
        && (!IPAddress.TryParse(uri.Host.Trim('[', ']'), out var literal) || WebhookAddressPolicy.IsAllowed(literal, allowPrivate));
}
