using Microsoft.Extensions.Logging;

namespace Notrelix.Platform.Messaging.Runtime.Governance;

public sealed class GovernanceEngine
{
    private readonly IEnumerable<IGovernancePolicy> _policies;
    private readonly ILogger<GovernanceEngine>? _logger;

    public GovernanceEngine(IEnumerable<IGovernancePolicy> policies, ILogger<GovernanceEngine>? logger = null)
    {
        _policies = policies;
        _logger = logger;
    }

    public async Task<IReadOnlyList<GovernanceResult>> EvaluateAsync(
        EventEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        var results = new List<GovernanceResult>();

        foreach (var policy in _policies)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await policy.EvaluateAsync(envelope, cancellationToken);

            _logger?.LogDebug(
                "Governance policy {Policy} evaluated event {Event}: {Decision}",
                policy.Name, envelope.EventName, result.Decision);

            results.Add(result);

            if (result.Decision == GovernanceDecision.Block)
            {
                _logger?.LogWarning(
                    "Governance policy {Policy} blocked event {Event}: {Reason}",
                    policy.Name, envelope.EventName, result.Reason);
                break;
            }
        }

        return results.AsReadOnly();
    }
}
