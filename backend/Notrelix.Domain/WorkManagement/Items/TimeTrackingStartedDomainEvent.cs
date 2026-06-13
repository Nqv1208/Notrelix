using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Items;

public record TimeTrackingStartedDomainEvent : DomainEvent
{
    public Guid BoardId { get; }
    public Guid ItemId { get; }
    public Guid EntryId { get; }

    public TimeTrackingStartedDomainEvent(
        Guid workspaceId,
        Guid boardId,
        Guid itemId,
        Guid entryId,
        Guid? actorUserId,
        DateTimeOffset occurredAt) 
        : base(occurredAt, workspaceId, actorUserId)
    {
        BoardId = boardId;
        ItemId = itemId;
        EntryId = entryId;
    }
}
