namespace Notrelix.Domain.Identity.Security.Events;

public record SsoProviderRestoredDomainEvent : DomainEvent
{
    public Guid ProviderId { get; }

    public SsoProviderRestoredDomainEvent(
        Guid workspaceId,
        Guid providerId,
        Guid? actorUserId,
        DateTimeOffset occurredAt)
        : base(occurredAt, workspaceId, actorUserId)
    {
        ProviderId = providerId;
    }
}
