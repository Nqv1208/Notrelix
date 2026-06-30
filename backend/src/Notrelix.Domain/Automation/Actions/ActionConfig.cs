namespace Notrelix.Domain.Automation.Actions;

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
