using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Security;

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
