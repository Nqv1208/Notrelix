using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Security;

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
