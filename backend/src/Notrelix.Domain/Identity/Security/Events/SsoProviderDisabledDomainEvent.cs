namespace Notrelix.Domain.Identity.Security.Events;

public record SsoProviderDisabledDomainEvent : DomainEvent
{
    public Guid ProviderId { get; }

    public SsoProviderDisabledDomainEvent(
        Guid workspaceId,
        Guid providerId,
        Guid? actorUserId,
        DateTimeOffset occurredAt)
        : base(occurredAt, workspaceId, actorUserId)
    {
        ProviderId = providerId;
    }
}
