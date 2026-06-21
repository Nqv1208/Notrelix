using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Items;

public class ItemSubscriber : Entity, IWorkspaceScoped
{
    public Guid WorkspaceId { get; private set; }
    public Guid BoardId { get; private set; }
    public Guid ItemId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid? SubscribedBy { get; private set; }
    public DateTimeOffset SubscribedAt { get; private set; }
    public long Version { get; private set; } = 1;

    private ItemSubscriber() : base() { }

    public static ItemSubscriber Create(
        Guid workspaceId,
        Guid boardId,
        Guid itemId,
        Guid userId,
        Guid? subscribedBy,
        DateTimeOffset subscribedAt)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(boardId);
        Guard.NotEmpty(itemId);
        Guard.NotEmpty(userId);

        return new ItemSubscriber
        {
            WorkspaceId = workspaceId,
            BoardId = boardId,
            ItemId = itemId,
            UserId = userId,
            SubscribedBy = subscribedBy,
            SubscribedAt = subscribedAt
        };
    }
}
