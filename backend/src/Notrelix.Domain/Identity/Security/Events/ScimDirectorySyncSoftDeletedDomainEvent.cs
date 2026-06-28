namespace Notrelix.Domain.Identity.Security.Events;

public record ScimDirectorySyncSoftDeletedDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid SyncId { get; }

    public ScimDirectorySyncSoftDeletedDomainEvent(
        Guid workspaceId,
        Guid syncId,
        Guid? actorUserId,
        DateTimeOffset occurredAt)
        : base(workspaceId, occurredAt, actorUserId)
    {
        SyncId = syncId;
    }
}
