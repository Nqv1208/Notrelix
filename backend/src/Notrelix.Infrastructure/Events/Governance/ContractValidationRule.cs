namespace Notrelix.Infrastructure.Events.Governance;

public sealed class ContractValidationRule : IGovernanceRule
{
    private readonly IContractRegistry _contractRegistry;

    public ContractValidationRule(IContractRegistry contractRegistry)
    {
        _contractRegistry = contractRegistry;
    }

    public string Name => "ContractValidation";

    public Task<GovernanceResult> EvaluateAsync(EventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        try
        {
            var contract = _contractRegistry.Get(envelope.EventName, envelope.EventVersion);

            if (contract.Deprecated)
            {
                return Task.FromResult(
                    GovernanceResult.Warn(Name,
                        $"Event '{envelope.EventName}' v{envelope.EventVersion} is deprecated." +
                        (contract.DeprecationDate.HasValue
                            ? $" Deprecated on {contract.DeprecationDate.Value:yyyy-MM-dd}."
                            : "")));
            }

            return Task.FromResult(GovernanceResult.Allow(Name));
        }
        catch (InvalidOperationException ex)
        {
            return Task.FromResult(
                GovernanceResult.Block(Name, $"No contract registered: {ex.Message}"));
        }
    }
}
