using Notrelix.Infrastructure.Integrations.Connections;

namespace Notrelix.Infrastructure.Messaging.Consumers.Integrations;

/// <summary>
/// The outbound webhook redelivery budget (owner decision 45: up to 8 attempts per event, then Failed and visible;
/// UC-INT-F06). The consumer definition and the consumer both read these numbers, so there is one place to change them.
/// </summary>
public static class WebhookRedeliveryPolicy
{
    /// <summary>Attempts per delivery, the first one included.</summary>
    public const int MaxAttempts = 8;

    /// <summary>Redeliveries after the first attempt: one wait per redelivery.</summary>
    public const int MaxRedeliveries = MaxAttempts - 1;

    /// <summary>
    /// The wait before each redelivery, in order (exponential backoff). Owner question: the decision names the budget but not
    /// the intervals; these are the proposal. Each wait is held in process by the retry pipeline, so long waits need a
    /// broker-side delayed redelivery first (owner decision on the transport).
    /// </summary>
    public static readonly IReadOnlyList<TimeSpan> RedeliveryIntervals =
    [
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(30),
        TimeSpan.FromMinutes(1),
        TimeSpan.FromMinutes(2),
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(10),
    ];

    /// <summary>
    /// Whether a failed attempt is redelivered: only a transient outcome, and only while the redeliveries already made
    /// (0 on the first attempt) are below the budget. Delivered and permanent outcomes are never redelivered.
    /// </summary>
    public static bool ShouldRedeliver(int redeliveriesMade, WebhookSendOutcome outcome) =>
        outcome == WebhookSendOutcome.Transient && redeliveriesMade < MaxRedeliveries;
}
