namespace Notrelix.Domain.Automation.Triggers;

/// <summary>
/// Experimental runtime trigger configuration. Schema and required properties
/// are not yet defined; frozen automation definitions use
/// <c>AutomationTriggerDefinition</c> in RulesEngine instead. Promotion
/// requires an accepted product/architecture decision under
/// <c>docs/governance/decision-and-exception-policy.md</c> §36, and isolation is
/// enforced by <c>ExperimentalRuntimeIsolationTests</c>.
/// </summary>
public sealed class TriggerConfig : ValueObject
{
    public JsonValue Data { get; private set; } = null!;

    private TriggerConfig() { }
    private TriggerConfig(JsonValue data)
    {
        Data = data;
    }

    public static TriggerConfig Create(JsonValue data)
    {
        Guard.NotNull(data);
        return new TriggerConfig(data);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Data;
    }
}
