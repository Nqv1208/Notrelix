using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Security;

public record ScimDirectorySyncPausedDomainEvent : DomainEvent
{
    public Guid SyncId { get; }

    public ScimDirectorySyncPausedDomainEvent(
        Guid workspaceId,
        Guid syncId,
        Guid? actorUserId,
        DateTimeOffset occurredAt) 
        : base(occurredAt, workspaceId, actorUserId)
    {
        SyncId = syncId;
    }
}
