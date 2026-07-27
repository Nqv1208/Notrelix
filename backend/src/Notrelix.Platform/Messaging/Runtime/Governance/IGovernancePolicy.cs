namespace Notrelix.Platform.Messaging.Runtime.Governance;

public enum GovernanceDecision
{
    Allow,
    Block,
    Warn,
}

public sealed record GovernanceResult
{
    public GovernanceDecision Decision { get; init; }
    public required string PolicyName { get; init; }
    public string? Reason { get; init; }

    public static GovernanceResult Allow(string policyName) =>
        new() { Decision = GovernanceDecision.Allow, PolicyName = policyName };

    public static GovernanceResult Block(string policyName, string reason) =>
        new() { Decision = GovernanceDecision.Block, PolicyName = policyName, Reason = reason };

    public static GovernanceResult Warn(string policyName, string reason) =>
        new() { Decision = GovernanceDecision.Warn, PolicyName = policyName, Reason = reason };
}

public interface IGovernancePolicy
{
    string Name { get; }
    Task<GovernanceResult> EvaluateAsync(EventEnvelope envelope, CancellationToken cancellationToken = default);
}
