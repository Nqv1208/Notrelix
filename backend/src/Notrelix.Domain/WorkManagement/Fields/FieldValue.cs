namespace Notrelix.Domain.WorkManagement.Fields;

public sealed class FieldValue : ValueObject
{
    public JsonValue Data { get; }

    private FieldValue() { }
    private FieldValue(JsonValue data)
    {
        Data = data;
    }

    public static FieldValue Create(JsonValue data)
    {
        Guard.NotNull(data);
        return new FieldValue(data);
    }

    public static FieldValue Empty() => new(JsonValue.Null());

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Data;
    }
}
