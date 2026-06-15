using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Security;

public record SsoProviderSoftDeletedDomainEvent : DomainEvent
{
    public Guid ProviderId { get; }

    public SsoProviderSoftDeletedDomainEvent(
        Guid workspaceId,
        Guid providerId,
        Guid? actorUserId,
        DateTimeOffset occurredAt) 
        : base(occurredAt, workspaceId, actorUserId)
    {
        ProviderId = providerId;
    }
}
