namespace Notrelix.Domain.WorkManagement.Fields;

public sealed class FieldSettings : ValueObject
{
    public JsonValue Data { get; } = null!;

    private FieldSettings() { }
    private FieldSettings(JsonValue data)
    {
        Data = data;
    }

    public static FieldSettings Create(JsonValue data)
    {
        Guard.NotNull(data);
        return new FieldSettings(data);
    }

    public static FieldSettings Empty() => new(JsonValue.EmptyObject());

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Data;
    }
}
