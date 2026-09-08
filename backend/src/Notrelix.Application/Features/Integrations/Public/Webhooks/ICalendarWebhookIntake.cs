namespace Notrelix.Application.Features.Integrations.Public.Webhooks;

/// <summary>
/// Outcome of an atomic technical-receipt claim.
/// </summary>
public enum CalendarWebhookIntakeResult
{
    /// <summary>This delivery won the claim and persisted the receipt.</summary>
    Accepted,

    /// <summary>An earlier delivery already holds the (provider, event id) identity — idempotent no-op.</summary>
    Duplicate,
}

/// <summary>
/// M8 AI-FLOW-07 — the technical receipt/dedup boundary for verified inbound
/// calendar webhooks. Infrastructure owns the physical receipt rows
/// (integrations.inbound_webhook_receipts); the Application depends only on
/// this port. Receipt identity is (provider, external event id) and the
/// database unique constraint is the dedup authority: concurrent claims
/// resolve to exactly one Accepted, losers observe Duplicate. The exact
/// verified raw bytes are hashed (SHA-256) and protected at rest.
/// </summary>
public interface ICalendarWebhookIntake
{
    /// <summary>
    /// Atomically claims the verified callback: one insert wins,
    /// the loser classifies as a duplicate — never an error path.
    /// </summary>
    Task<CalendarWebhookIntakeResult> AcceptAsync(
        string provider,
        string externalEventId,
        string rawBody,
        DateTimeOffset receivedAt,
        CancellationToken cancellationToken);

    /// <summary>
    /// Records a rejected callback (bad signature/timestamp) for bounded
    /// operational diagnostics only — never business processing state.
    /// </summary>
    Task RecordRejectedAsync(
        string provider,
        string rawBody,
        string reason,
        DateTimeOffset receivedAt,
        CancellationToken cancellationToken);
}
