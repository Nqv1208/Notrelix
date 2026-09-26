namespace Notrelix.Domain.Automation.Actions;

/// <summary>
/// Experimental runtime action configuration. Schema and required properties
/// are not yet defined; frozen automation definitions use
/// <c>AutomationActionDefinition</c> in RulesEngine instead. Promotion requires
/// an accepted product/architecture decision under
/// <c>docs/governance/decision-and-exception-policy.md</c> §36, and isolation is
/// enforced by <c>ExperimentalRuntimeIsolationTests</c>.
/// </summary>
public sealed class ActionConfig : ValueObject
{
    public JsonValue Data { get; private set; } = null!;

    private ActionConfig() { }
    private ActionConfig(JsonValue data)
    {
        Data = data;
    }

    public static ActionConfig Create(JsonValue data)
    {
        Guard.NotNull(data);
        return new ActionConfig(data);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Data;
    }
}
