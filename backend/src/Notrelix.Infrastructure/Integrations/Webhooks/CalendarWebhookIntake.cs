using Notrelix.Application.Features.Integrations.Public.Webhooks;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Integrations;
using Microsoft.Extensions.Logging.Abstractions;

namespace Notrelix.Infrastructure.Integrations.Webhooks;

/// <summary>
/// M8 AI-FLOW-07 — the technical receipt/dedup implementation behind the
/// Application port. Receipts are Infrastructure reliability state in
/// integrations.inbound_webhook_receipts. The database unique constraint on
/// (connection_id, provider, external event id) is the dedup authority: the
/// claim is a single conditional INSERT ... ON CONFLICT DO NOTHING RETURNING,
/// so the loser of a concurrent race observes an empty result and classifies
/// the delivery as a duplicate — never an error, and the ambient transaction
/// never enters an aborted state. Dedup is connection-scoped because the
/// provider event id only namespaces a delivery within a provider
/// calendar/connection — BE-API-041 only permits provider-event-id dedup
/// where the provider contract defines that uniqueness, and no repository
/// contract defines provider-wide ExternalEventId uniqueness. The ConnectionId
/// is the trusted binding identity resolved from the WebhookPath bootstrap;
/// it is never taken from the payload. The payload hash is SHA-256 over the
/// exact rawBody under the provider's UTF-8 contract; the raw payload is
/// persisted only encrypted at rest for accepted callbacks. Rejected/untrusted
/// callbacks produce bounded telemetry only.
///
/// TAC v2.6 WAVE-E — the claim is written NON-terminal ("Captured"): the
/// bootstrap only claims the delivery. The terminal receipt state
/// (Processed / Blocked / Failed) is decided by the tenant-scoped processing
/// consumer after commit, never by the intake.
/// </summary>
public sealed class CalendarWebhookIntake : ICalendarWebhookIntake
{
    /// <summary>The Data Protection purpose used to protect/unprotect the verified raw payload bytes.</summary>
    public const string PayloadProtectionPurpose = "Notrelix.Integrations.CalendarWebhooks.v1";

    private readonly ApplicationDbContext _context;
    private readonly ISecretEncryptor _encryptor;
    private readonly ILogger<CalendarWebhookIntake> _logger;

    public CalendarWebhookIntake(
        ApplicationDbContext context,
        ISecretEncryptor encryptor,
        ILogger<CalendarWebhookIntake>? logger = null)
    {
        _context = context;
        _encryptor = encryptor;
        _logger = logger ?? NullLogger<CalendarWebhookIntake>.Instance;
    }

    public async Task<CalendarWebhookIntakeResult> AcceptAsync(
        CalendarWebhookReceiptClaim claim,
        CancellationToken cancellationToken)
    {
        var receipt = InboundWebhookReceipt.Capture(
            claim.AccountId,
            claim.WorkspaceId,
            claim.ConnectionId,
            claim.Provider,
            claim.ProviderDeliveryId,
            ComputePayloadHash(claim.RawBody),
            Protect(claim.RawBody),
            claim.ReceivedAt);

        // The identity claim must be atomic inside the ambient data-session
        // transaction without gambling on a constraint violation aborting that
        // transaction: one conditional INSERT whose RETURNING result is the
        // claim authority. The conflict target infers the unique index
        // ux_inbound_webhook_receipts_connection_provider_external_event_id —
        // the dedup authority. An empty result means another delivery already
        // holds the (connection, provider, event id) identity — idempotent
        // no-op, no error, no poisoned transaction.
        var claimed = await _context.Database
            .SqlQuery<Guid?>($"""
                INSERT INTO integration.inbound_webhook_receipts
                    (id, account_id, workspace_id, connection_id, provider, external_event_id, payload_hash, protected_payload,
                     received_at, status, processed_at, terminal_at, failure_code, failure_detail)
                VALUES (
                    {receipt.Id}, {receipt.AccountId}, {receipt.WorkspaceId}, {receipt.ConnectionId}, {receipt.Provider}, {receipt.ExternalEventId}, {receipt.PayloadHash},
                    {receipt.ProtectedPayload}, {receipt.ReceivedAt}, {receipt.Status},
                    {receipt.ProcessedAt}, {receipt.TerminalAt}, {receipt.FailureCode}, {receipt.FailureDetail})
                ON CONFLICT (connection_id, provider, external_event_id) DO NOTHING
                RETURNING id
                """)
            .ToListAsync(cancellationToken);

        if (claimed.Count > 0)
        {
            // WAVE-E: the claim itself stays "Captured" (non-terminal). The
            // accepted receipt identity and payload hash drive the
            // provider-neutral processing message the handler enqueues.
            return new CalendarWebhookIntakeResult(
                CalendarWebhookIntakeOutcome.Accepted,
                receipt.Id,
                receipt.PayloadHash);
        }

        // Lost the identity race: the tracked entity must not be flushed as a
        // second INSERT when the surrounding session commits.
        _context.Entry(receipt).State = EntityState.Detached;
        return new CalendarWebhookIntakeResult(
            CalendarWebhookIntakeOutcome.Duplicate,
            null,
            null);
    }

    public async Task RecordRejectedAsync(
        string provider,
        string rawBody,
        string reason,
        DateTimeOffset receivedAt,
        CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "Rejected calendar webhook callback. Provider={Provider}, Reason={Reason}, PayloadHash={PayloadHash}, PayloadSize={PayloadSize}, ReceivedAt={ReceivedAt}",
            provider,
            reason,
            ComputePayloadHash(rawBody),
            System.Text.Encoding.UTF8.GetByteCount(rawBody),
            receivedAt);
        await Task.CompletedTask;
    }

    /// <summary>SHA-256 over the exact rawBody — the same UTF-8 bytes the signature was verified against.</summary>
    private static string ComputePayloadHash(string rawBody) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(rawBody)));

    private string Protect(string rawBody) => _encryptor.Protect(rawBody, PayloadProtectionPurpose);
}
