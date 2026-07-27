namespace Notrelix.Domain.WorkManagement.Items;

/// <summary>
/// Pre-loaded snapshot of an item's parent chain, supplied by Application.
/// Domain uses this for cycle detection without callbacks.
/// </summary>
public sealed class ItemParentSnapshot
{
    public Guid ItemId { get; init; }
    public Guid BoardId { get; init; }
    public Guid? ParentItemId { get; init; }

    public ItemParentSnapshot(Guid itemId, Guid boardId, Guid? parentItemId)
    {
        ItemId = itemId;
        BoardId = boardId;
        ParentItemId = parentItemId;
    }
}

/// <summary>
/// Pre-loaded snapshot of an item's dependency chain, supplied by Application.
/// Domain uses this for cycle detection without callbacks.
/// </summary>
public sealed class ItemDependencySnapshot
{
    public Guid ItemId { get; init; }
    public IReadOnlyList<Guid> DependencyIds { get; init; }

    public ItemDependencySnapshot(Guid itemId, IReadOnlyList<Guid> dependencyIds)
    {
        ItemId = itemId;
        DependencyIds = dependencyIds;
    }
}
