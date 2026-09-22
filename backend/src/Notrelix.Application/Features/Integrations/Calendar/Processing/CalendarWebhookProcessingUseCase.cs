namespace Notrelix.Application.Features.Integrations.Calendar.Processing;

/// <summary>
/// TAC v2.6 WAVE-E — the default implementation of the provider-neutral
/// processing seam. The semantic translation of a provider calendar callback
/// into a product target action (and the target action itself) is NOT yet
/// defined at this milestone (WAVE-E BLOCKED-DECISION). Per the no-guess
/// policy, this seam MUST NOT invent a mutation, a target aggregate, or a
/// provider-neutral canonical payload shape. It therefore reports
/// <see cref="CalendarWebhookProcessingOutcome.SemanticTargetUndefined"/>,
/// which the receipt consumer records durably as an explicit "Blocked"
/// terminal state — proving the split transport + real-RLS execution, while
/// never falsely marking the delivery Processed and never spin-retrying an
/// undefined target.
/// </summary>
public sealed class CalendarWebhookProcessingUseCase : ICalendarWebhookProcessingUseCase
{
    public Task<CalendarWebhookProcessingOutcome> ProcessAsync(
        CalendarWebhookProcessingInput input,
        CancellationToken cancellationToken)
    {
        // The downstream semantic target for a verified calendar callback is
        // undocumented (TAC v2.6 WAVE-E BLOCKED-DECISION, TYPE:
        // SemanticsExtendBeyondDocumentedContract). Do not invent it here.
        return Task.FromResult(CalendarWebhookProcessingOutcome.SemanticTargetUndefined);
    }
}