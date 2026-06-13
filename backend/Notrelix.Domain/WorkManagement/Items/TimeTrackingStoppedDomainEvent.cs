using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Items;

public record TimeTrackingStoppedDomainEvent : DomainEvent
{
    public Guid BoardId { get; }
    public Guid ItemId { get; }
    public Guid EntryId { get; }
    public int DurationSeconds { get; }

    public TimeTrackingStoppedDomainEvent(
        Guid workspaceId,
        Guid boardId,
        Guid itemId,
        Guid entryId,
        int durationSeconds,
        Guid? actorUserId,
        DateTimeOffset occurredAt) 
        : base(occurredAt, workspaceId, actorUserId)
    {
        BoardId = boardId;
        ItemId = itemId;
        EntryId = entryId;
        DurationSeconds = durationSeconds;
    }
}
