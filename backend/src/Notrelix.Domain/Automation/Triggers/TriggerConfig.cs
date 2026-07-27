namespace Notrelix.Domain.Automation.Triggers;

/// <summary>
/// Experimental — runtime trigger configuration. Schema and required properties are not yet defined.
/// Frozen automation definitions use AutomationTriggerDefinition in RulesEngine instead.
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
