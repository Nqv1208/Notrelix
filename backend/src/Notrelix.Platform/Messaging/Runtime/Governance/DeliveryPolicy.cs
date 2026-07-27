using Notrelix.Application.Common.Events;

namespace Notrelix.Platform.Messaging.Runtime.Governance;

public sealed class DeliveryPolicy : IGovernancePolicy
{
    public string Name => "Delivery";

    public Task<GovernanceResult> EvaluateAsync(
        EventEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        if (envelope.Classification == EventClassification.Internal)
            return Task.FromResult(GovernanceResult.Warn(Name,
                $"Internal event {envelope.EventName} should use direct delivery, not outbox"));

        return Task.FromResult(GovernanceResult.Allow(Name));
    }
}
