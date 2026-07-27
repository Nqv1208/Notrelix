namespace Notrelix.Platform.Messaging.Runtime.Governance;

public sealed class RetentionPolicy : IGovernancePolicy
{
    public string Name => "Retention";

    public Task<GovernanceResult> EvaluateAsync(
        EventEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GovernanceResult.Allow(Name));
    }
}
