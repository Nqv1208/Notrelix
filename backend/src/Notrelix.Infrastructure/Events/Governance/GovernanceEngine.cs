namespace Notrelix.Infrastructure.Events.Governance;

public sealed class GovernanceEngine
{
    private readonly IEnumerable<IGovernanceRule> _rules;

    public GovernanceEngine(IEnumerable<IGovernanceRule> rules)
    {
        _rules = rules;
    }

    public async Task<GovernanceResult> EvaluateAsync(EventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        foreach (var rule in _rules)
        {
            var result = await rule.EvaluateAsync(envelope, cancellationToken);

            if (result.Decision == GovernanceDecision.Block)
                return result;

            if (result.Decision == GovernanceDecision.Warn)
            {
            }
        }

        return GovernanceResult.Allow("GovernanceEngine");
    }
}
