namespace Notrelix.Domain.WorkManagement.Fields;

public sealed class FieldValue : ValueObject
{
    public JsonValue Data { get; } = null!;

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

    /// <summary>The canonical cleared value (JSON null); valid for every writable field type.</summary>
    public bool IsEmpty => Data is null || Data.Value == "null";

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Data;
    }
}
