namespace Notrelix.Domain.Identity.Security.Events;

public record ScimDirectorySyncCreatedDomainEvent : DomainEvent
{
    public Guid SyncId { get; }
    public string ProviderName { get; }

    public ScimDirectorySyncCreatedDomainEvent(
        Guid workspaceId,
        Guid syncId,
        string providerName,
        Guid? actorUserId,
        DateTimeOffset occurredAt)
        : base(occurredAt, workspaceId, actorUserId)
    {
        SyncId = syncId;
        ProviderName = providerName;
    }
}
