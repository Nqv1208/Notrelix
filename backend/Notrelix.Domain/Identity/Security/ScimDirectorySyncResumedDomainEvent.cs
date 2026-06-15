using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Security;

public record ScimDirectorySyncResumedDomainEvent : DomainEvent
{
    public Guid SyncId { get; }

    public ScimDirectorySyncResumedDomainEvent(
        Guid workspaceId,
        Guid syncId,
        Guid? actorUserId,
        DateTimeOffset occurredAt) 
        : base(occurredAt, workspaceId, actorUserId)
    {
        SyncId = syncId;
    }
}
