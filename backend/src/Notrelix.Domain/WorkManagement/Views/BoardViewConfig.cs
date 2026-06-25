namespace Notrelix.Domain.WorkManagement.Views;

public class BoardViewConfig : ValueObject
{
    public JsonValue Data { get; }

    private BoardViewConfig() { }
    protected BoardViewConfig(JsonValue data)
    {
        Data = data;
    }

    public static BoardViewConfig Create(JsonValue data)
    {
        return new BoardViewConfig(data);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Data;
    }
}
