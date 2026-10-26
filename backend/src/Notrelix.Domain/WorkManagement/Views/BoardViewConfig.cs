namespace Notrelix.Domain.WorkManagement.Views;

/// <summary>
/// Presentation configuration of a BoardView, kept as one JSON document. The
/// view owns how Items are shown, never the Item data. Keys the domain knows
/// are checked when a configuration is built (today <c>wipLimits</c>, see
/// <see cref="KanbanWipLimits"/>); other keys are carried as given.
/// </summary>
public class BoardViewConfig : ValueObject
{
    public JsonValue Data { get; } = null!;

    private BoardViewConfig() { }
    protected BoardViewConfig(JsonValue data)
    {
        Guard.NotNull(data);
        KanbanWipLimits.FromConfig(data);
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
