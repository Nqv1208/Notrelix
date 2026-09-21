namespace Notrelix.Application.Features.Integrations.Calendar.Processing;

/// <summary>
/// TAC v2.6 WAVE-E — the provider-neutral input for calendar webhook
/// processing. It reaches the seam already verified, decrypted, provenance-
/// bound (never payload-derived) and running under the derived tenant. The raw
/// callback is exchanged for this product-semantic-neutral shape at the
/// seam boundary: downstream code must not depend on the HTTP/signature layer.
/// </summary>
public sealed record CalendarWebhookProcessingInput(
    Guid ReceiptId,
    Guid ConnectionId,
    string Provider,
    string ExternalEventId,
    string PayloadHash,
    DateTimeOffset ReceivedAt,
    string DecryptedRawPayload);

/// <summary>
/// The outcome of the provider-neutral processing seam. It decides the DURABLE
/// terminal receipt state — the bootstrap never decides it.
/// </summary>
public enum CalendarWebhookProcessingOutcome
{
    /// <summary>The downstream semantic target action was applied (future completed seam).</summary>
    Completed = 1,

    /// <summary>
    /// The provider-neutral input reached the processing seam, but the
    /// downstream semantic translation/target action is not yet defined
    /// (TAC v2.6 WAVE-E BLOCKED-DECISION). The consumer records this durably
    /// as an explicit non-Processed terminal state ("Blocked") — never a false
    /// success and never retried.
    /// </summary>
    SemanticTargetUndefined = 2,

    /// <summary>A transient technical failure — the delivery may be retried safely.</summary>
    RetryableFailure = 3,
}

/// <summary>
/// TAC v2.6 WAVE-E — the Integrations Application seam that consumes the
/// provider-neutral webhook input under the real derived tenant. The verified,
/// decrypted callback arrives here stripped of the transport boundary. The
/// consumer maps <see cref="CalendarWebhookProcessingOutcome"/> onto the durable
/// receipt lifecycle; the seam itself owns no Infrastructure state.
/// </summary>
public interface ICalendarWebhookProcessingUseCase
{
    Task<CalendarWebhookProcessingOutcome> ProcessAsync(
        CalendarWebhookProcessingInput input,
        CancellationToken cancellationToken);
}