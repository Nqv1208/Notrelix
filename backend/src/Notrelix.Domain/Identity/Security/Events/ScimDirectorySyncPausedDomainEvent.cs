namespace Notrelix.Domain.Identity.Security.Events;

public record ScimDirectorySyncPausedDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid SyncId { get; }

    public ScimDirectorySyncPausedDomainEvent(
        Guid workspaceId,
        Guid syncId,
        Guid? actorUserId,
        DateTimeOffset occurredAt)
        : base(workspaceId, occurredAt, actorUserId)
    {
        SyncId = syncId;
    }
}
