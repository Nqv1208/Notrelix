namespace Notrelix.Infrastructure.Messaging;

/// <summary>
/// PF-08 explicit ordering declaration for a production message consumer.
///
/// Every <c>MassTransit.IConsumer&lt;T&gt;</c> in this assembly MUST carry this
/// attribute (enforced by an Architecture test), so a new consumer cannot skip the
/// ordering decision. It states that no business or mechanism invariant of the
/// consumer depends on the delivery order of its messages, and why.
///
/// It is NOT a transport setting. The production delivery runtime is MassTransit
/// plus the dedup filter; <c>Platform.ConsumerHost</c>, <c>OrderingEnforcer</c> and
/// <c>PoisonDetector</c> are not in that graph, and no integration event carries a
/// contiguous per-stream sequence they could enforce. A consumer that genuinely
/// needs ordered delivery therefore cannot be declared here: it needs the ordering
/// owner, stream identity and sequence producer to be decided first (see
/// platform-and-messaging.md, "Production consumer ordering declaration").
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class OrderingNotApplicableAttribute : Attribute
{
    public OrderingNotApplicableAttribute(string reason)
    {
        Reason = reason;
    }

    /// <summary>One-line justification of why delivery order cannot change the outcome.</summary>
    public string Reason { get; }
}

/// <summary>Shared justifications for consumers that share the same basis.</summary>
internal static class OrderingReasons
{
    public const string LogOnly =
        "Log-only consumer: it logs the fact only and has no state effect, so delivery order cannot change any outcome; re-declare when a real effect is added.";
}
