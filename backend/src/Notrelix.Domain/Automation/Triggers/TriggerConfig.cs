namespace Notrelix.Domain.Automation.Triggers;

public sealed class TriggerConfig : ValueObject
{
    public JsonValue Data { get; }

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
