using Notrelix.Application.Features.Integrations.Public.Webhooks;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Integrations;

namespace Notrelix.Infrastructure.Integrations.Webhooks;

/// <summary>
/// M8 AI-FLOW-07 — the technical receipt/dedup implementation behind the
/// Application port. Receipts are Infrastructure reliability state in
/// integrations.inbound_webhook_receipts; dedup identity is (provider,
/// external event id) enforced by the unique index and checked here.
/// </summary>
public sealed class CalendarWebhookIntake : ICalendarWebhookIntake
{
    private readonly ApplicationDbContext _context;
    private readonly IDateTimeProvider _clock;

    public CalendarWebhookIntake(ApplicationDbContext context, IDateTimeProvider clock)
    {
        _context = context;
        _clock = clock;
    }

    public async Task AcceptAsync(
        string provider,
        string externalEventId,
        DateTimeOffset receivedAt,
        CancellationToken cancellationToken)
    {
        var duplicate = await _context.InboundWebhookReceipts
            .AsNoTracking()
            .AnyAsync(r =>
                r.Provider == provider
                && r.ExternalEventId == externalEventId
                && r.Status == "Processed", cancellationToken);

        if (duplicate)
        {
            // Duplicate delivery: recorded as a rejected duplicate for bounded
            // diagnostics — no second business effect is produced.
            _context.InboundWebhookReceipts.Add(InboundWebhookReceipt.CaptureRejected(
                provider,
                "duplicate",
                "duplicate delivery",
                _clock.UtcNow));
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        var receipt = InboundWebhookReceipt.Capture(
            provider, externalEventId, ComputeReceiptHash(provider, externalEventId), receivedAt);
        receipt.MarkProcessed(_clock.UtcNow);
        _context.InboundWebhookReceipts.Add(receipt);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RecordRejectedAsync(
        string provider,
        string reason,
        DateTimeOffset receivedAt,
        CancellationToken cancellationToken)
    {
        _context.InboundWebhookReceipts.Add(InboundWebhookReceipt.CaptureRejected(
            provider,
            ComputeReceiptHash(provider, reason),
            reason,
            receivedAt));
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static string ComputeReceiptHash(string provider, string externalEventId) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes($"{provider}:{externalEventId}")));
}
