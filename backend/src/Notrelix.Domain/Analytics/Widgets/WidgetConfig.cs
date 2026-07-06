namespace Notrelix.Domain.Analytics.Widgets;

public sealed class WidgetConfig : ValueObject
{
    public JsonValue Data { get; private set; } = null!;

    private WidgetConfig() { }
    private WidgetConfig(JsonValue data)
    {
        Data = data;
    }

    public static WidgetConfig Create(JsonValue data)
    {
        Guard.NotNull(data);
        return new WidgetConfig(data);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Data;
    }
}
