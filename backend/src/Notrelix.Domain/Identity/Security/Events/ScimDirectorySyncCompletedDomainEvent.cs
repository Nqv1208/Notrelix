namespace Notrelix.Domain.Identity.Security.Events;

public record ScimDirectorySyncCompletedDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid SyncId { get; }

    public ScimDirectorySyncCompletedDomainEvent(
        Guid workspaceId,
        Guid syncId,
        DateTimeOffset occurredAt)
        : base(workspaceId, occurredAt)
    {
        SyncId = syncId;
    }
}
