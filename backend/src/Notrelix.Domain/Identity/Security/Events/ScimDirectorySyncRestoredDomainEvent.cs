namespace Notrelix.Domain.Identity.Security.Events;

public record ScimDirectorySyncRestoredDomainEvent : DomainEvent
{
    public Guid SyncId { get; }

    public ScimDirectorySyncRestoredDomainEvent(
        Guid workspaceId,
        Guid syncId,
        Guid? actorUserId,
        DateTimeOffset occurredAt)
        : base(occurredAt, workspaceId, actorUserId)
    {
        SyncId = syncId;
    }
}
