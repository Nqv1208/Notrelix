using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Security.Events;

public record ScimDirectorySyncSoftDeletedDomainEvent : DomainEvent
{
    public Guid SyncId { get; }

    public ScimDirectorySyncSoftDeletedDomainEvent(
        Guid workspaceId,
        Guid syncId,
        Guid? actorUserId,
        DateTimeOffset occurredAt) 
        : base(occurredAt, workspaceId, actorUserId)
    {
        SyncId = syncId;
    }
}
