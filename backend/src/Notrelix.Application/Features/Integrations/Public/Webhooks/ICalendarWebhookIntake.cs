namespace Notrelix.Application.Features.Integrations.Public.Webhooks;

/// <summary>
/// M8 AI-FLOW-07 — the technical receipt/dedup boundary for verified inbound
/// calendar webhooks. Infrastructure owns the physical receipt rows
/// (integrations.inbound_webhook_receipts); the Application depends only on
/// this port. Dedup identity: (provider, external event id).
/// </summary>
public interface ICalendarWebhookIntake
{
    /// <summary>
    /// Records an accepted, verified callback. A duplicate (same provider +
    /// external event id, already processed) is a no-op — no second business
    /// effect is produced.
    /// </summary>
    Task AcceptAsync(
        string provider,
        string externalEventId,
        DateTimeOffset receivedAt,
        CancellationToken cancellationToken);

    /// <summary>
    /// Records a rejected callback for bounded operational diagnostics only —
    /// never business processing state.
    /// </summary>
    Task RecordRejectedAsync(
        string provider,
        string reason,
        DateTimeOffset receivedAt,
        CancellationToken cancellationToken);
}
