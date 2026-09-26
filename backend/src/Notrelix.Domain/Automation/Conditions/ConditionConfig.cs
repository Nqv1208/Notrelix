namespace Notrelix.Domain.Automation.Conditions;

/// <summary>
/// Experimental runtime condition configuration. Schema and required
/// properties are not yet defined; frozen automation definitions use
/// <c>AutomationConditionDefinition</c> in RulesEngine instead. Promotion
/// requires an accepted product/architecture decision under
/// <c>docs/governance/decision-and-exception-policy.md</c> §36, and isolation is
/// enforced by <c>ExperimentalRuntimeIsolationTests</c>.
/// </summary>
public sealed class ConditionConfig : ValueObject
{
    public JsonValue Data { get; private set; } = null!;

    private ConditionConfig() { }
    private ConditionConfig(JsonValue data)
    {
        Data = data;
    }

    public static ConditionConfig Create(JsonValue data)
    {
        Guard.NotNull(data);
        return new ConditionConfig(data);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Data;
    }
}
