namespace Notrelix.Platform.Messaging.Runtime.Governance;

public sealed class CompliancePolicy : IGovernancePolicy
{
    public string Name => "Compliance";

    public Task<GovernanceResult> EvaluateAsync(
        EventEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GovernanceResult.Allow(Name));
    }
}
