namespace Notrelix.Domain.Identity.Security.Events;

public record ScimDirectorySyncResumedDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid SyncId { get; }

    public ScimDirectorySyncResumedDomainEvent(
        Guid workspaceId,
        Guid syncId,
        Guid? actorUserId,
        DateTimeOffset occurredAt)
        : base(workspaceId, occurredAt, actorUserId)
    {
        SyncId = syncId;
    }
}
