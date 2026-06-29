namespace Notrelix.Domain.Identity.Security.Events;

public record SsoProviderRestoredDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid ProviderId { get; }

    public SsoProviderRestoredDomainEvent(
        Guid workspaceId,
        Guid providerId,
        Guid? actorUserId,
        DateTimeOffset occurredAt)
        : base(workspaceId, occurredAt, actorUserId)
    {
        ProviderId = providerId;
    }
}
