namespace Notrelix.Infrastructure.Security.Webhooks;

/// <summary>
/// Skeleton webhook signature service (v4 §8.3 / §12). Outbound webhooks must be
/// signed; inbound webhooks must verify signature and be idempotent. A concrete
/// HMAC signer for n8n already exists in the Application layer; this is the
/// general-purpose Infrastructure home. Not yet wired.
/// </summary>
public sealed class WebhookSignatureService
{
    // TODO(v4 §12): Sign(payload, secret) + Verify(payload, signature, secret)
    // with constant-time comparison; per-provider signing schemes.
}
