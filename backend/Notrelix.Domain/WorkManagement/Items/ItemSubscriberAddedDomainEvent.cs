using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Items;

public record ItemSubscriberAddedDomainEvent : DomainEvent
{
    public Guid BoardId { get; }
    public Guid ItemId { get; }
    public Guid UserId { get; }

    public ItemSubscriberAddedDomainEvent(
        Guid workspaceId,
        Guid boardId,
        Guid itemId,
        Guid userId,
        Guid? actorUserId,
        DateTimeOffset occurredAt) 
        : base(occurredAt, workspaceId, actorUserId)
    {
        BoardId = boardId;
        ItemId = itemId;
        UserId = userId;
    }
}
