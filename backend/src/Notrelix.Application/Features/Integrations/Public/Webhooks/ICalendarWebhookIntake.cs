namespace Notrelix.Application.Features.Integrations.Public.Webhooks;

/// <summary>
/// Outcome of an atomic technical-receipt claim.
/// </summary>
public enum CalendarWebhookIntakeOutcome
{
    /// <summary>This delivery won the claim and persisted the receipt.</summary>
    Accepted,

    /// <summary>An earlier delivery already holds the (connection, provider, event id) identity — idempotent no-op.</summary>
    Duplicate,
}

/// <summary>
/// Result of the atomic technical-receipt claim. Only an <see cref="Accepted"/>
/// delivery carries a <see cref="ReceiptId"/> (the stable claim identity for the
/// provider-neutral processing message) and the derived <see cref="PayloadHash"/>
/// (the SHA-256 over the exact verified raw bytes).
/// </summary>
public sealed record CalendarWebhookIntakeResult(
    CalendarWebhookIntakeOutcome Outcome,
    Guid? ReceiptId,
    string? PayloadHash)
{
    public bool Succeeded => Outcome == CalendarWebhookIntakeOutcome.Accepted;
}

/// <summary>
/// Trusted receipt provenance assembled after provider verification and binding
/// resolution. The intake persists this object; the later processing consumer
/// must compare its message copy with the persisted receipt before invoking any
/// semantic target.
/// </summary>
public sealed record CalendarWebhookReceiptClaim(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ConnectionId,
    string Provider,
    string ProviderDeliveryId,
    string RawBody,
    DateTimeOffset ReceivedAt);

/// <summary>
/// M8 AI-FLOW-07 — the technical receipt/dedup boundary for verified inbound
/// calendar webhooks. Infrastructure owns the physical receipt rows
/// (integrations.inbound_webhook_receipts); the Application depends only on
/// this port. Receipt identity is (connection, provider, external event id) —
/// the provider event id only namespaces a delivery within a provider
/// calendar/connection (BE-API-041 requires provider-contract dedup scope) —
/// and the database unique constraint is the dedup authority: concurrent
/// claims resolve to exactly one Accepted, losers observe Duplicate. The
/// exact verified raw bytes are hashed (SHA-256) and protected at rest.
/// </summary>
public interface ICalendarWebhookIntake
{
    /// <summary>
    /// Atomically claims the verified callback: one insert wins,
    /// the loser classifies as a duplicate — never an error path. The
    /// connectionId is the trusted provenance/binding identity resolved from
    /// the WebhookPath → CalendarIntegration → IntegrationConnection bootstrap;
    /// it is required for accepted receipts and never derived from the payload.
    /// The claim is intentionally NON-terminal (status "Captured"): terminal
    /// receipt state (Processed / Blocked / Failed) is decided by the
    /// tenant-scoped processing seam after commit, never by the bootstrap.
    /// </summary>
    Task<CalendarWebhookIntakeResult> AcceptAsync(
        CalendarWebhookReceiptClaim claim,
        CancellationToken cancellationToken);

    /// <summary>
    /// Records bounded telemetry for a rejected callback. Implementations must
    /// not create an InboundWebhookReceipt or persist the raw body.
    /// </summary>
    Task RecordRejectedAsync(
        string provider,
        string rawBody,
        string reason,
        DateTimeOffset receivedAt,
        CancellationToken cancellationToken);
}
