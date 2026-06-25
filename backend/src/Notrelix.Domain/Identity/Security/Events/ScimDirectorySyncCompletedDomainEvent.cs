namespace Notrelix.Domain.Identity.Security.Events;

public record ScimDirectorySyncCompletedDomainEvent : DomainEvent
{
    public Guid SyncId { get; }

    public ScimDirectorySyncCompletedDomainEvent(
        Guid workspaceId,
        Guid syncId,
        DateTimeOffset occurredAt)
        : base(occurredAt, workspaceId, null)
    {
        SyncId = syncId;
    }
}
