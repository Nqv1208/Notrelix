namespace Notrelix.Domain.Identity.Security.Events;

public record SsoProviderCreatedDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid ProviderId { get; }
    public string Name { get; }

    public SsoProviderCreatedDomainEvent(
        Guid workspaceId,
        Guid providerId,
        string name,
        Guid? actorUserId,
        DateTimeOffset occurredAt)
        : base(workspaceId, occurredAt, actorUserId)
    {
        ProviderId = providerId;
        Name = name;
    }
}
