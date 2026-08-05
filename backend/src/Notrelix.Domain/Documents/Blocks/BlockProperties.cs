namespace Notrelix.Domain.Documents.Blocks;

public sealed class BlockProperties : ValueObject
{
    public JsonValue Data { get; } = null!;

    private BlockProperties() { }
    private BlockProperties(JsonValue data)
    {
        Data = data;
    }

    public static BlockProperties Create(JsonValue data)
    {
        Guard.NotNull(data);
        return new BlockProperties(data);
    }

    public static BlockProperties Empty() => new(JsonValue.EmptyObject());

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Data;
    }
}
