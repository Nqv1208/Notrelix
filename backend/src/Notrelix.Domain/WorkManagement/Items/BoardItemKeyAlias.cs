namespace Notrelix.Domain.WorkManagement.Items;

/// <summary>
/// The key an Item held on a board it has since left. A moved Item gets the target board's next key; its old key
/// keeps resolving to the Item through this row (a prefix change applies to new Items only).
/// </summary>
public class BoardItemKeyAlias : Entity, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    /// <summary>The board the key was issued on.</summary>
    public Guid BoardId { get; private set; }
    public Guid ItemId { get; private set; }
    public string ItemKey { get; private set; } = string.Empty;

    private BoardItemKeyAlias() : base() { }

    public static BoardItemKeyAlias Create(Guid accountId, Guid workspaceId, Guid boardId, Guid itemId, string itemKey) =>
        new() { AccountId = accountId, WorkspaceId = workspaceId, BoardId = boardId, ItemId = itemId, ItemKey = itemKey };
}
