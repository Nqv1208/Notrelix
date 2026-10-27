namespace Notrelix.Application.Features.WorkManagement.ItemLinks;

/// <summary>
/// How a stored link reads from either of its items. A link row is directed: a Dependency row
/// "source is blocked by target"; a Reference row relates two items both ways. The viewed item
/// therefore sees <c>blocked-by</c> when it is the source of a Dependency row, <c>blocks</c> when
/// it is the target, and <c>related</c> for a Reference row.
/// </summary>
public static class ItemLinkRelations
{
    public const string BlockedBy = "blocked-by";
    public const string Blocks = "blocks";
    public const string Related = "related";

    public static string ToRelation(BoardItemLinkType type, bool viewedItemIsSource) => type switch
    {
        BoardItemLinkType.Dependency => viewedItemIsSource ? BlockedBy : Blocks,
        _ => Related,
    };

    /// <summary>The stored row for "<paramref name="itemId"/> has <paramref name="relation"/> with <paramref name="otherId"/>".</summary>
    public static bool TryToRow(string relation, Guid itemId, Guid otherId, out Guid sourceId, out Guid targetId, out BoardItemLinkType type)
    {
        switch (relation)
        {
            case BlockedBy:
                (sourceId, targetId, type) = (itemId, otherId, BoardItemLinkType.Dependency);
                return true;
            case Blocks:
                (sourceId, targetId, type) = (otherId, itemId, BoardItemLinkType.Dependency);
                return true;
            case Related:
                (sourceId, targetId, type) = (itemId, otherId, BoardItemLinkType.Reference);
                return true;
            default:
                (sourceId, targetId, type) = (default, default, default);
                return false;
        }
    }
}
