namespace Notrelix.Domain.Identity.Security.Events;

public record SsoProviderEnabledDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid ProviderId { get; }
    public string Name { get; }

    public SsoProviderEnabledDomainEvent(
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
