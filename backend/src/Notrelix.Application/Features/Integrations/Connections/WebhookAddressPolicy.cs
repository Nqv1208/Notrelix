using System.Net;
using System.Net.Sockets;

namespace Notrelix.Application.Features.Integrations.Connections;

/// <summary>
/// Which addresses an outgoing webhook may reach (SSRF guard). Link-local addresses (cloud metadata
/// included), unspecified, multicast and broadcast are never allowed; loopback and private networks
/// only where <c>allowPrivate</c> (development).
/// </summary>
public static class WebhookAddressPolicy
{
    public static bool IsAllowed(IPAddress address, bool allowPrivate)
    {
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();

        if (address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any)
            || address.Equals(IPAddress.Broadcast) || address.Equals(IPAddress.None))
            return false;

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (address.IsIPv6LinkLocal || address.IsIPv6Multicast || address.IsIPv6SiteLocal) return false;
            if (IPAddress.IsLoopback(address)) return allowPrivate;
            var bytes = address.GetAddressBytes();
            var uniqueLocal = (bytes[0] & 0xFE) == 0xFC;
            return allowPrivate || !uniqueLocal;
        }

        var b = address.GetAddressBytes();
        if (b[0] == 169 && b[1] == 254) return false; // link-local, cloud metadata
        if (b[0] >= 224) return false; // multicast and reserved
        if (b[0] == 0) return false;
        if (IPAddress.IsLoopback(address)) return allowPrivate;

        var isPrivate = b[0] == 10
            || (b[0] == 172 && b[1] >= 16 && b[1] <= 31)
            || (b[0] == 192 && b[1] == 168)
            || (b[0] == 100 && b[1] >= 64 && b[1] <= 127); // carrier-grade NAT
        return allowPrivate || !isPrivate;
    }

    /// <summary>The checks that need no DNS: scheme, shape, and an address given as a literal.</summary>
    public static string? ValidateShape(string? url, bool allowInsecure, bool allowPrivate)
    {
        if (string.IsNullOrWhiteSpace(url) || url.Length > 2048) return "The address is missing or too long.";
        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)) return "The address is not a valid URL.";
        if (uri.Scheme != Uri.UriSchemeHttps && !(allowInsecure && uri.Scheme == Uri.UriSchemeHttp))
            return "The address must use https.";
        if (!string.IsNullOrEmpty(uri.UserInfo)) return "The address must not contain credentials.";
        if (string.IsNullOrEmpty(uri.Host)) return "The address has no host.";
        if (IPAddress.TryParse(uri.Host.Trim('[', ']'), out var literal) && !IsAllowed(literal, allowPrivate))
            return "The address points to a network the webhook may not reach.";
        if (!allowPrivate && uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            return "The address points to a network the webhook may not reach.";
        return null;
    }
}
