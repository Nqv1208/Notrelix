namespace Notrelix.Domain.Automation.Conditions;

/// <summary>
/// Experimental — runtime condition configuration. Schema and required properties are not yet defined.
/// Frozen automation definitions use AutomationConditionDefinition in RulesEngine instead.
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
