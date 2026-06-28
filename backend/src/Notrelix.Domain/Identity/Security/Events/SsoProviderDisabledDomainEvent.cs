namespace Notrelix.Domain.Identity.Security.Events;

public record SsoProviderDisabledDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid ProviderId { get; }

    public SsoProviderDisabledDomainEvent(
        Guid workspaceId,
        Guid providerId,
        Guid? actorUserId,
        DateTimeOffset occurredAt)
        : base(workspaceId, occurredAt, actorUserId)
    {
        ProviderId = providerId;
    }
}
