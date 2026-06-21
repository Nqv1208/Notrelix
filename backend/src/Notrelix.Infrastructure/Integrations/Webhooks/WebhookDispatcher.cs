namespace Notrelix.Infrastructure.Integrations.Webhooks;

/// <summary>
/// Skeleton outbound webhook dispatcher (v4 §12). Real implementation signs the
/// payload, delivers with retry/backoff via a worker, and records delivery status.
/// Provider-specific SDKs stay in Infrastructure. Not yet wired.
/// </summary>
public sealed class WebhookDispatcher
{
    // TODO(v4 §12): sign + POST with retry; persist WebhookDelivery; dead-letter.
}
