using Notrelix.Application.Common.Diagnostics;
using Notrelix.Application.Common.Security;
using Notrelix.Application.Events.Integrations;
using Notrelix.Application.Features.Integrations.Calendar.Processing;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Integrations;
using Notrelix.Infrastructure.Integrations.Webhooks;

namespace Notrelix.Infrastructure.Messaging.Consumers.Integrations;

/// <summary>
/// TAC v2.6 WAVE-E — tenant-scoped inbound adapter for a durable
/// <see cref="CalendarWebhookProcessingRequestedV1"/>. The receive pipeline
/// (TenantContextConsumeFilter → DeduplicationConsumeFilter) restores the
/// workspace tenant carried in the envelope and applies it to the real RLS
/// session (set_config within the wrapping transaction) BEFORE this consumer
/// runs — this is the post-verification real-RLS step, not an in-memory
/// adoption. The consumer then loads the claimed receipt, decrypts the
/// payload inside that tenant session, executes the Integrations Application
/// seam, and DURABLY decides the terminal receipt state:
///
/// <list type="bullet">
///   <item>Completed → "Processed" (semantic success, future seam).</item>
///   <item>SemanticTargetUndefined → "Blocked" — explicit terminal, never a
///   false Processed and never retried (durable BLOCKED-DECISION state).</item>
///   <item>RetryableFailure → retryable exception; the wrapping dedup claim
///   rolls back so delivery retries under the same message identity.</item>
///   <item>Non-retryable technical failure (missing/corrupt payload) →
///   "Failed" terminal so the delivery does not poison-loop.</item>
/// </list>
///
/// A redelivered message whose receipt is already terminal is a converged
/// no-op: the wrapping dedup claim is the exact-once identity and the receipt
/// state is the reconciliation truth. The consumer owns no business rules.
/// </summary>
public sealed class CalendarWebhookProcessingRequestedConsumer
    : IConsumer<CalendarWebhookProcessingRequestedV1>
{
    private readonly ApplicationDbContext _db;
    private readonly ICalendarWebhookProcessingUseCase _useCase;
    private readonly ISecretEncryptor _encryptor;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<CalendarWebhookProcessingRequestedConsumer> _logger;
    private readonly PipelineMetrics _metrics;

    public CalendarWebhookProcessingRequestedConsumer(
        ApplicationDbContext db,
        ICalendarWebhookProcessingUseCase useCase,
        ISecretEncryptor encryptor,
        IDateTimeProvider clock,
        ILogger<CalendarWebhookProcessingRequestedConsumer> logger,
        PipelineMetrics? metrics = null)
    {
        _db = db;
        _useCase = useCase;
        _encryptor = encryptor;
        _clock = clock;
        _logger = logger;
        _metrics = metrics ?? new PipelineMetrics();
    }

    public async Task Consume(ConsumeContext<CalendarWebhookProcessingRequestedV1> context)
    {
        var message = context.Message;
        var ct = context.CancellationToken;

        var receipt = await _db.InboundWebhookReceipts
            .FirstOrDefaultAsync(r => r.Id == message.ReceiptId, ct);

        if (receipt is null)
        {
            _logger.LogWarning(
                "Calendar webhook processing for receipt {ReceiptId} could not find the claimed receipt; skipping",
                message.ReceiptId);
            return;
        }

        if (receipt.Status != "Captured")
        {
            _logger.LogDebug(
                "Calendar webhook receipt {ReceiptId} already {Status}; convergent no-op",
                message.ReceiptId, receipt.Status);
            return;
        }

        // Decrypt the verified raw payload inside the derived tenant session.
        // A missing/corrupt protected payload is a durable non-retryable
        // failure — never a false Processed and never a poison retry loop.
        if (receipt.ProtectedPayload is null)
        {
            receipt.MarkFailed(
                "receipt has no protected payload to decrypt",
                _clock.UtcNow);
            await _db.SaveChangesAsync(ct);
            return;
        }

        string decrypted;
        try
        {
            decrypted = _encryptor.Unprotect(
                receipt.ProtectedPayload,
                CalendarWebhookIntake.PayloadProtectionPurpose);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Calendar webhook receipt {ReceiptId} protected payload could not be decrypted",
                message.ReceiptId);
            receipt.MarkFailed(
                "protected payload could not be decrypted",
                _clock.UtcNow);
            await _db.SaveChangesAsync(ct);
            return;
        }

        var outcome = await _useCase.ProcessAsync(
            new CalendarWebhookProcessingInput(
                message.ReceiptId,
                message.ConnectionId,
                message.Provider,
                message.ExternalEventId,
                message.PayloadHash,
                message.ReceivedAt,
                decrypted),
            ct);

        switch (outcome)
        {
            case CalendarWebhookProcessingOutcome.Completed:
                receipt.MarkProcessed(_clock.UtcNow);
                break;

            case CalendarWebhookProcessingOutcome.SemanticTargetUndefined:
                // Durable explicit BLOCKED-DECISION outcome: not Processed,
                // not retried. The reason is stable for reconciliation.
                receipt.MarkBlocked(
                    "semantic target undefined (TAC v2.6 WAVE-E BLOCKED-DECISION)",
                    _clock.UtcNow);
                break;

            case CalendarWebhookProcessingOutcome.RetryableFailure:
                _logger.LogWarning(
                    "Calendar webhook processing for receipt {ReceiptId} requires another delivery attempt",
                    message.ReceiptId);
                throw new CalendarWebhookProcessingRetryableException(message.ReceiptId);

            default:
                throw new ArgumentOutOfRangeException(nameof(outcome), outcome, "Unknown processing outcome.");
        }

        await _db.SaveChangesAsync(ct);
    }
}

/// <summary>
/// Signals the delivery mechanism to redeliver the processing intent. Carries
/// the stable receipt identity for diagnostics only.
/// </summary>
public sealed class CalendarWebhookProcessingRetryableException(Guid receiptId)
    : InvalidOperationException(
        $"calendar webhook processing for receipt {receiptId} requires another delivery attempt.");