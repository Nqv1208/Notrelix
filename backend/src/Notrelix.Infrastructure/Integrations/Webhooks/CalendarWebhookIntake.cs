using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Security;
using Notrelix.Application.Common.Time;
using Notrelix.Application.Features.Integrations.Public.Webhooks;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Integrations;

namespace Notrelix.Infrastructure.Integrations.Webhooks;

/// <summary>
/// M8 AI-FLOW-07 — the technical receipt/dedup implementation behind the
/// Application port. Receipts are Infrastructure reliability state in
/// integrations.inbound_webhook_receipts. The database unique constraint on
/// (provider, external event id) is the dedup authority: the claim is a
/// single INSERT, and the loser of a concurrent race observes the constraint
/// and classifies the delivery as a duplicate — never an error. The payload
/// hash is SHA-256 over the exact verified raw bytes; the raw payload is
/// persisted only encrypted at rest.
/// </summary>
public sealed class CalendarWebhookIntake : ICalendarWebhookIntake
{
    private const string ReceiptIdentityConstraint = "ux_inbound_webhook_receipts_provider_external_event_id";

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
        _context.InboundWebhookReceipts.Add(receipt);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return CalendarWebhookIntakeResult.Accepted;
        }
        catch (DbUpdateException ex) when (IsIdentityConflict(ex))
        {
            // Concurrent duplicate claim: another delivery already holds the
            // (provider, event id) identity — idempotent no-op, no error.
            _context.Entry(receipt).State = EntityState.Detached;
            return CalendarWebhookIntakeResult.Duplicate;
        }
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

    /// <summary>SHA-256 over the exact raw bytes used for signature verification.</summary>
    private static string ComputePayloadHash(string rawBody) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(rawBody)));

    private string Protect(string rawBody) => _encryptor.Protect(rawBody, "Notrelix.Integrations.CalendarWebhooks.v1");

    private static bool IsIdentityConflict(DbUpdateException ex) =>
        ex.InnerException is Npgsql.PostgresException
        {
            SqlState: "23505"
        } pg && pg.ConstraintName == ReceiptIdentityConstraint;
}
