namespace Notrelix.Domain.Documents.Blocks;

public sealed class BlockContent : ValueObject
{
    public JsonValue Data { get; }

    private BlockContent() { }
    private BlockContent(JsonValue data)
    {
        Data = data;
    }

    public static BlockContent Create(JsonValue data)
    {
        Guard.NotNull(data);
        return new BlockContent(data);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Data;
    }
}
