namespace Notrelix.Infrastructure.Events.Governance;

public sealed class TraceContextValidationRule : IGovernanceRule
{
    public string Name => "TraceContextValidation";

    public Task<GovernanceResult> EvaluateAsync(EventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(envelope.CorrelationId) && string.IsNullOrWhiteSpace(envelope.TraceParent))
        {
            return Task.FromResult(
                GovernanceResult.Warn(Name,
                    $"Event '{envelope.EventName}' {envelope.Id} has no CorrelationId or TraceParent."));
        }

        if (!string.IsNullOrWhiteSpace(envelope.TraceParent))
        {
            var parts = envelope.TraceParent.Split('-');
            if (parts.Length != 4)
            {
                return Task.FromResult(
                    GovernanceResult.Block(Name,
                        $"Invalid TraceParent format in event '{envelope.EventName}' {envelope.Id}."));
            }
        }

        return Task.FromResult(GovernanceResult.Allow(Name));
    }
}
