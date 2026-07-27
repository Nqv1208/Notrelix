using Notrelix.Application.Common.Events;

namespace Notrelix.Platform.Messaging.Runtime.Governance;

public sealed class ClassificationPolicy : IGovernancePolicy
{
    public string Name => "Classification";

    public Task<GovernanceResult> EvaluateAsync(
        EventEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        if (envelope.Classification is EventClassification.Audit or EventClassification.Internal)
            return Task.FromResult(GovernanceResult.Warn(Name,
                $"Event {envelope.EventName} is classified as {envelope.Classification} — " +
                "ensure it is not routed to external consumers"));

        return Task.FromResult(GovernanceResult.Allow(Name));
    }
}
