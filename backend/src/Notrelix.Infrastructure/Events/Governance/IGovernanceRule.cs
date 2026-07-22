namespace Notrelix.Infrastructure.Events.Governance;

public enum GovernanceDecision
{
    Allow,
    Block,
    Warn,
}

public sealed record GovernanceResult
{
    public GovernanceDecision Decision { get; init; }
    public string RuleName { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;

    public static GovernanceResult Allow(string ruleName) =>
        new() { Decision = GovernanceDecision.Allow, RuleName = ruleName };

    public static GovernanceResult Block(string ruleName, string reason) =>
        new() { Decision = GovernanceDecision.Block, RuleName = ruleName, Reason = reason };

    public static GovernanceResult Warn(string ruleName, string reason) =>
        new() { Decision = GovernanceDecision.Warn, RuleName = ruleName, Reason = reason };
}

public interface IGovernanceRule
{
    string Name { get; }
    Task<GovernanceResult> EvaluateAsync(EventEnvelope envelope, CancellationToken cancellationToken = default);
}
