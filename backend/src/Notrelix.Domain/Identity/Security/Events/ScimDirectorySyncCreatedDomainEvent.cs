namespace Notrelix.Domain.Identity.Security.Events;

public record ScimDirectorySyncCreatedDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid SyncId { get; }
    public string ProviderName { get; }

    public ScimDirectorySyncCreatedDomainEvent(
        Guid workspaceId,
        Guid syncId,
        string providerName,
        Guid? actorUserId,
        DateTimeOffset occurredAt)
        : base(workspaceId, occurredAt, actorUserId)
    {
        SyncId = syncId;
        ProviderName = providerName;
    }
}
