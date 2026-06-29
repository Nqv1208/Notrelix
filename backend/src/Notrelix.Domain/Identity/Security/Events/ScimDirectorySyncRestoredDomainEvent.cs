namespace Notrelix.Domain.Identity.Security.Events;

public record ScimDirectorySyncRestoredDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid SyncId { get; }

    public ScimDirectorySyncRestoredDomainEvent(
        Guid workspaceId,
        Guid syncId,
        Guid? actorUserId,
        DateTimeOffset occurredAt)
        : base(workspaceId, occurredAt, actorUserId)
    {
        SyncId = syncId;
    }
}
