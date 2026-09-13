using Notrelix.Application.Features.Integrations.Public.Webhooks;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Integrations;

namespace Notrelix.Infrastructure.Integrations.Webhooks;

/// <summary>
/// M8 AI-FLOW-07 — the technical receipt/dedup implementation behind the
/// Application port. Receipts are Infrastructure reliability state in
/// integrations.inbound_webhook_receipts. The database unique constraint on
/// (provider, external event id) is the dedup authority: the claim is a
/// single conditional INSERT ... ON CONFLICT DO NOTHING RETURNING, so the
/// loser of a concurrent race observes an empty result and classifies the
/// delivery as a duplicate — never an error, and the ambient transaction
/// never enters an aborted state. The payload hash is SHA-256 over the exact
/// rawBody under the provider's UTF-8 contract; the raw payload is persisted
/// only encrypted at rest.
/// </summary>
public sealed class CalendarWebhookIntake : ICalendarWebhookIntake
{
    private readonly ApplicationDbContext _context;
    private readonly ISecretEncryptor _encryptor;
    private readonly IDateTimeProvider _clock;

    public CalendarWebhookIntake(
        ApplicationDbContext context,
        ISecretEncryptor encryptor,
        IDateTimeProvider clock)
    {
        _context = context;
        _encryptor = encryptor;
        _clock = clock;
    }

    public async Task<CalendarWebhookIntakeResult> AcceptAsync(
        string provider,
        string externalEventId,
        string rawBody,
        DateTimeOffset receivedAt,
        CancellationToken cancellationToken)
    {
        var receipt = InboundWebhookReceipt.Capture(
            provider,
            externalEventId,
            ComputePayloadHash(rawBody),
            Protect(rawBody),
            receivedAt);
        // Intake is the business of this flow: claiming the receipt IS the
        // processed technical effect (AI-FLOW-07 is frozen intake-only).
        receipt.MarkProcessed(_clock.UtcNow);

        // The identity claim must be atomic inside the ambient data-session
        // transaction without gambling on a constraint violation aborting that
        // transaction: one conditional INSERT whose RETURNING result is the
        // claim authority. The conflict target infers the unique index
        // ux_inbound_webhook_receipts_provider_external_event_id — the dedup
        // authority. An empty result means another delivery already holds the
        // (provider, event id) identity — idempotent no-op, no error, no
        // poisoned transaction.
        var claimed = await _context.Database
            .SqlQuery<Guid?>($"""
                INSERT INTO integration.inbound_webhook_receipts
                    (id, provider, external_event_id, payload_hash, protected_payload,
                     received_at, status, processed_at, failure_reason)
                VALUES (
                    {receipt.Id}, {receipt.Provider}, {receipt.ExternalEventId}, {receipt.PayloadHash},
                    {receipt.ProtectedPayload}, {receipt.ReceivedAt}, {receipt.Status},
                    {receipt.ProcessedAt}, {receipt.FailureReason})
                ON CONFLICT (provider, external_event_id) DO NOTHING
                RETURNING id
                """)
            .ToListAsync(cancellationToken);

        if (claimed.Count > 0)
        {
            return CalendarWebhookIntakeResult.Accepted;
        }

        // Lost the identity race: the tracked entity must not be flushed as a
        // second INSERT when the surrounding session commits.
        _context.Entry(receipt).State = EntityState.Detached;
        return CalendarWebhookIntakeResult.Duplicate;
    }

    public async Task RecordRejectedAsync(
        string provider,
        string rawBody,
        string reason,
        DateTimeOffset receivedAt,
        CancellationToken cancellationToken)
    {
        _context.InboundWebhookReceipts.Add(InboundWebhookReceipt.CaptureRejected(
            provider,
            ComputePayloadHash(rawBody),
            Protect(rawBody),
            reason,
            receivedAt));
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>SHA-256 over the exact rawBody — the same UTF-8 bytes the signature was verified against.</summary>
    private static string ComputePayloadHash(string rawBody) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(rawBody)));

    private string Protect(string rawBody) => _encryptor.Protect(rawBody, "Notrelix.Integrations.CalendarWebhooks.v1");
}
