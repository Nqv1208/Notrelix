namespace Notrelix.Domain.WorkManagement.Items;

/// <summary>
/// When one board member last opened an Item ("Đã xem"). One row per Item and person. Guests are never
/// recorded; Application decides that. Writing is throttled by <see cref="Throttle"/> so opening an Item is cheap.
/// </summary>
public class BoardItemView : Entity, IWorkspaceScoped
{
    /// <summary>A person's row is rewritten at most this often.</summary>
    public static readonly TimeSpan Throttle = TimeSpan.FromMinutes(5);

    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid BoardId { get; private set; }
    public Guid ItemId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTimeOffset LastViewedAt { get; private set; }

    private BoardItemView() : base() { }

    public static BoardItemView Create(Guid accountId, Guid workspaceId, Guid boardId, Guid itemId, Guid userId, DateTimeOffset viewedAt)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(boardId);
        Guard.NotEmpty(itemId);
        Guard.NotEmpty(userId);
        return new BoardItemView
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            BoardId = boardId,
            ItemId = itemId,
            UserId = userId,
            LastViewedAt = viewedAt,
        };
    }

    /// <summary>Records a newer view; false (nothing changed) while the last one is inside the throttle window.</summary>
    public bool Touch(DateTimeOffset viewedAt)
    {
        if (viewedAt - LastViewedAt < Throttle) return false;
        LastViewedAt = viewedAt;
        return true;
    }
}
