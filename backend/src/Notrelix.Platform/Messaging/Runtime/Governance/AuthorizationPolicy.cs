namespace Notrelix.Platform.Messaging.Runtime.Governance;

public sealed class AuthorizationPolicy : IGovernancePolicy
{
    public string Name => "Authorization";

    public Task<GovernanceResult> EvaluateAsync(
        EventEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GovernanceResult.Allow(Name));
    }
}
