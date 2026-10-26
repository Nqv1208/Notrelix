using System.Security.Cryptography;
using System.Text;

namespace Notrelix.Application.Features.Integrations.Connections;

/// <summary>
/// How a delivery is signed: HMAC-SHA256 over `{timestamp}.{body}` with the connection's signing secret,
/// sent as `X-Notrelix-Signature: sha256=&lt;hex&gt;` next to `X-Notrelix-Timestamp`.
/// </summary>
public static class WebhookSignature
{
    public const string SignatureHeader = "X-Notrelix-Signature";
    public const string TimestampHeader = "X-Notrelix-Timestamp";
    public const string DeliveryHeader = "X-Notrelix-Delivery";
    public const string EventHeader = "X-Notrelix-Event";

    public static string Compute(string secret, string timestamp, string body)
    {
        var key = Encoding.UTF8.GetBytes(secret);
        var message = Encoding.UTF8.GetBytes($"{timestamp}.{body}");
        return "sha256=" + Convert.ToHexString(HMACSHA256.HashData(key, message)).ToLowerInvariant();
    }

    /// <summary>A new signing secret: 32 random bytes, shown to the member once.</summary>
    public static string GenerateSecret()
        => "whsec_" + Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
